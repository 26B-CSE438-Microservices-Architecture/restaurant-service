namespace RestaurantService.API.IntegrationEvents;

public record ProductStockChangedEvent(
    Guid ProductId,
    Guid RestaurantId,
    bool IsAvailable,
    DateTime Timestamp
);
