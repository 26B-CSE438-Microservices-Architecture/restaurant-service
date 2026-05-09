using MassTransit;
using Microsoft.EntityFrameworkCore;
using RestaurantService.API.Data;
using RestaurantService.API.IntegrationEvents;
using RestaurantService.API.IntegrationEvents.Inbound;
using RestaurantService.API.Services;

namespace RestaurantService.API.Consumers;

/// <summary>
/// order-service'den gelen "ORDER_CANCELLED" event'ini dinler.
/// Asıl COMPENSATION akışı burada: rezerve edilmiş stok geri açılır,
/// StockReleasedEvent yayınlanır. İdempotent — aynı event 2 kez gelirse 2. kez no-op.
/// </summary>
public class OrderCancelledEventConsumer : IConsumer<OrderCancelledIntegrationEvent>
{
    private readonly IStockReservationService _reservationService;
    private readonly AppDbContext _db;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<OrderCancelledEventConsumer> _logger;

    public OrderCancelledEventConsumer(
        IStockReservationService reservationService,
        AppDbContext db,
        IPublishEndpoint publishEndpoint,
        ILogger<OrderCancelledEventConsumer> logger)
    {
        _reservationService = reservationService;
        _db = db;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCancelledIntegrationEvent> context)
    {
        var msg = context.Message;
        var payload = msg?.Payload;

        if (payload is null || !Guid.TryParse(payload.OrderId, out var orderId))
        {
            _logger.LogWarning("Geçersiz OrderCancelled payload: {Raw}", msg);
            return;
        }

        var reason = payload.CancelledBy ?? "UNKNOWN";

        _logger.LogInformation(
            "ORDER_CANCELLED alındı. OrderId={OrderId} CancelledBy={Reason} RefundRequired={Refund}",
            orderId, reason, payload.RefundRequired);

        // Release'den ÖNCE restaurantId'yi rezervasyondan çıkaralım (event payload'ında yok)
        var restaurantId = await _db.StockReservations
            .Where(r => r.OrderId == orderId)
            .Select(r => (Guid?)r.RestaurantId)
            .FirstOrDefaultAsync(context.CancellationToken) ?? Guid.Empty;

        var result = await _reservationService.ReleaseAsync(orderId, reason, context.CancellationToken);

        if (result.ReleasedItems.Count > 0)
        {
            await _publishEndpoint.Publish(new StockReleasedEvent(
                orderId,
                restaurantId,
                result.ReleasedItems.Select(i => new ReleasedItemDto(i.ProductId, i.Quantity)).ToList(),
                reason,
                DateTime.UtcNow
            ), context.CancellationToken);

            _logger.LogInformation(
                "COMPENSATION tamamlandı: Order {OrderId} için {Count} ürünün stoğu geri açıldı.",
                orderId, result.ReleasedItems.Count);
        }
        else
        {
            _logger.LogInformation(
                "Order {OrderId} için aktif rezervasyon yoktu, compensation gerekmedi.", orderId);
        }
    }
}
