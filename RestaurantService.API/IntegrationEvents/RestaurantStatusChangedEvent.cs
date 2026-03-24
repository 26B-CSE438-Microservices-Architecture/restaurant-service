namespace RestaurantService.API.IntegrationEvents;

public record RestaurantStatusChangedEvent(
    Guid RestaurantId,
    string OldStatus,
    string NewStatus,
    DateTime Timestamp
);
