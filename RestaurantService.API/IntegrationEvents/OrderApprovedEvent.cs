namespace RestaurantService.API.IntegrationEvents;

/// <summary>
/// Restoran siparişi async olarak onayladığında yayınlanır.
/// İleride order-service bu event'i dinleyip REST endpoint yerine state transition yapabilir.
/// </summary>
public record OrderApprovedEvent(
    Guid OrderId,
    Guid RestaurantId,
    int EstimatedPrepMinutes,
    DateTime ApprovedAt
);
