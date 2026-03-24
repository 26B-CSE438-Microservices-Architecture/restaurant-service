namespace RestaurantService.API.IntegrationEvents;

public record RestaurantCreatedEvent(
    Guid RestaurantId,
    string Name,
    double Latitude,
    double Longitude,
    DateTime Timestamp
);
