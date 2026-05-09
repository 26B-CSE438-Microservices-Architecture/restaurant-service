namespace RestaurantService.API.IntegrationEvents;

/// <summary>
/// Restoran siparişi reddettiğinde (yetersiz stok, kapalı, vs.) yayınlanır.
/// order-service bunu compensation tetikleyici olarak kullanmalıdır:
/// payment hold release + ORDER_CANCELLED.
/// </summary>
public record OrderRejectedEvent(
    Guid OrderId,
    Guid RestaurantId,
    string RejectionReason,
    DateTime RejectedAt
);
