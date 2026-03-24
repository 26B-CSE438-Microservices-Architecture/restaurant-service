namespace RestaurantService.API.IntegrationEvents;

public record PriceChangedEvent(
    Guid ProductId,
    Guid RestaurantId,
    decimal OldPrice,
    decimal NewPrice,
    DateTime Timestamp
);
