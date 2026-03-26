using System;

namespace RestaurantService.API.IntegrationEvents.Inbound;

public record OrderCompletedEvent(Guid OrderId, Guid RestaurantId, decimal TotalAmount, DateTime Timestamp);
