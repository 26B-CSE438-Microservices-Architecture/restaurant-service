namespace RestaurantService.API.IntegrationEvents;

public record RestaurantDeletedEvent(
    Guid RestaurantId,
    DateTime Timestamp
);
