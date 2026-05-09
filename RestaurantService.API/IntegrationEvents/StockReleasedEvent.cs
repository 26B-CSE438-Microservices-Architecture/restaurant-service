namespace RestaurantService.API.IntegrationEvents;

public record ReleasedItemDto(Guid ProductId, int Quantity);

/// <summary>
/// Compensation event'i: rezerve edilen stok başarıyla geri açıldığında yayınlanır.
/// </summary>
public record StockReleasedEvent(
    Guid OrderId,
    Guid RestaurantId,
    List<ReleasedItemDto> Items,
    string Reason,
    DateTime ReleasedAt
);
