using Microsoft.EntityFrameworkCore;
using RestaurantService.API.Data;
using RestaurantService.API.Entities;
using RestaurantService.API.IntegrationEvents.Inbound;

namespace RestaurantService.API.Services;

public class StockReservationServiceImpl : IStockReservationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<StockReservationServiceImpl> _logger;

    public StockReservationServiceImpl(AppDbContext db, ILogger<StockReservationServiceImpl> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ReservationResult> TryReserveAsync(
        Guid orderId,
        Guid restaurantId,
        IReadOnlyList<OrderItemPayloadDto> items,
        CancellationToken ct = default)
    {
        // Idempotency: bu sipariş için zaten rezervasyon yapılmış mı?
        var existing = await _db.StockReservations
            .Where(r => r.OrderId == orderId)
            .ToListAsync(ct);

        if (existing.Count > 0)
        {
            _logger.LogInformation("Order {OrderId} için rezervasyon zaten mevcut, idempotent skip.", orderId);
            var alreadyReserved = existing
                .Where(r => r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.Confirmed)
                .Select(r => (r.ProductId, r.Quantity))
                .ToList();
            return new ReservationResult(true, null, alreadyReserved);
        }

        // Restoran kontrolü
        var restaurant = await _db.Restaurants.FirstOrDefaultAsync(r => r.Id == restaurantId, ct);
        if (restaurant is null)
            return new ReservationResult(false, $"Restoran bulunamadı: {restaurantId}", Array.Empty<(Guid, int)>());

        if (!restaurant.IsActive || restaurant.Status != RestaurantStatus.Open)
            return new ReservationResult(false, "Restoran şu anda siparişe kapalı", Array.Empty<(Guid, int)>());

        // Tüm ürünleri tek seferde çek
        var productIds = items.Select(i => Guid.Parse(i.MenuItemId)).Distinct().ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        // Önce TÜM stoğu doğrula (all-or-nothing)
        foreach (var item in items)
        {
            var pid = Guid.Parse(item.MenuItemId);
            if (!products.TryGetValue(pid, out var product))
                return new ReservationResult(false, $"Ürün bulunamadı: {pid}", Array.Empty<(Guid, int)>());

            if (!product.IsAvailable)
                return new ReservationResult(false, $"Ürün stokta değil: {product.Name}", Array.Empty<(Guid, int)>());

            if (product.StockQuantity < item.Quantity)
                return new ReservationResult(false,
                    $"Yetersiz stok: {product.Name} (mevcut: {product.StockQuantity}, istenen: {item.Quantity})",
                    Array.Empty<(Guid, int)>());
        }

        // Hepsi OK → transaction içinde rezerve et
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var now = DateTime.UtcNow;
            var reservedItems = new List<(Guid, int)>();

            foreach (var item in items)
            {
                var pid = Guid.Parse(item.MenuItemId);
                var product = products[pid];

                product.StockQuantity -= item.Quantity;

                _db.StockReservations.Add(new StockReservation
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    RestaurantId = restaurantId,
                    ProductId = pid,
                    Quantity = item.Quantity,
                    Status = ReservationStatus.Reserved,
                    CreatedAt = now
                });

                reservedItems.Add((pid, item.Quantity));
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            _logger.LogInformation("Order {OrderId} için {Count} ürün rezerve edildi.", orderId, reservedItems.Count);
            return new ReservationResult(true, null, reservedItems);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _logger.LogError(ex, "Order {OrderId} rezervasyonu sırasında hata.", orderId);
            return new ReservationResult(false, "Rezervasyon sırasında beklenmeyen hata", Array.Empty<(Guid, int)>());
        }
    }

    public async Task<ReleaseResult> ReleaseAsync(Guid orderId, string reason, CancellationToken ct = default)
    {
        var reservations = await _db.StockReservations
            .Where(r => r.OrderId == orderId && r.Status == ReservationStatus.Reserved)
            .ToListAsync(ct);

        if (reservations.Count == 0)
        {
            _logger.LogInformation("Order {OrderId} için release edilecek rezervasyon yok (idempotent).", orderId);
            return new ReleaseResult(Array.Empty<(Guid, int)>());
        }

        var productIds = reservations.Select(r => r.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var now = DateTime.UtcNow;
            var releasedItems = new List<(Guid, int)>();

            foreach (var r in reservations)
            {
                if (products.TryGetValue(r.ProductId, out var product))
                {
                    product.StockQuantity += r.Quantity;
                }

                r.Status = ReservationStatus.Released;
                r.UpdatedAt = now;
                releasedItems.Add((r.ProductId, r.Quantity));
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            _logger.LogInformation("Order {OrderId} için {Count} rezervasyon geri açıldı. Sebep: {Reason}",
                orderId, releasedItems.Count, reason);
            return new ReleaseResult(releasedItems);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _logger.LogError(ex, "Order {OrderId} release sırasında hata.", orderId);
            throw;
        }
    }

    public async Task ConfirmAsync(Guid orderId, CancellationToken ct = default)
    {
        var reservations = await _db.StockReservations
            .Where(r => r.OrderId == orderId && r.Status == ReservationStatus.Reserved)
            .ToListAsync(ct);

        if (reservations.Count == 0)
        {
            _logger.LogInformation("Order {OrderId} için confirm edilecek rezervasyon yok (idempotent).", orderId);
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var r in reservations)
        {
            r.Status = ReservationStatus.Confirmed;
            r.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Order {OrderId} için {Count} rezervasyon Confirmed yapıldı.", orderId, reservations.Count);
    }
}
