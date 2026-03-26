using System;

namespace RestaurantService.API.IntegrationEvents.Inbound;

public record UserRoleChangedEvent(Guid UserId, string NewRole, DateTime Timestamp);
