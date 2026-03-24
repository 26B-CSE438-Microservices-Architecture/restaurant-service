namespace RestaurantService.API.IntegrationEvents;

public record RestaurantUpdatedEvent(
    Guid RestaurantId,
    string[] ChangedFields,
    DateTime Timestamp
);
