namespace RestaurantService.API.IntegrationEvents;

public record ReservedItemDto(Guid ProductId, int Quantity);

/// <summary>
/// Sipariş için stok başarıyla rezerve edildiğinde yayınlanır.
/// Audit / observability amaçlı; başka servisler de dinleyebilir.
/// </summary>
public record StockReservedEvent(
    Guid OrderId,
    Guid RestaurantId,
    List<ReservedItemDto> Items,
    DateTime ReservedAt
);
