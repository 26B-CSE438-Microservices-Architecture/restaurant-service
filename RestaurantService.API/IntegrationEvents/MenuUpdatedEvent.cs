namespace RestaurantService.API.IntegrationEvents;

public record MenuUpdatedEvent(
    Guid RestaurantId,
    string Action,
    string EntityType,
    Guid EntityId,
    DateTime Timestamp
);
