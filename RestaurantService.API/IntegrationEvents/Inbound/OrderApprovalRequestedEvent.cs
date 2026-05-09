using System.Text.Json.Serialization;

namespace RestaurantService.API.IntegrationEvents.Inbound;

/// <summary>
/// order-service'den gelen "order.restaurant.approval.requested" event'i.
/// Spring publisher şu zarf yapısını kullanıyor:
/// {
///   "eventId": "...",
///   "eventType": "ORDER_RESTAURANT_APPROVAL_REQUESTED",
///   "correlationId": "...",
///   "occurredAt": "2026-...",
///   "payload": { orderId, restaurantId, userId, totalAmount: {amount,currency}, items: [...] }
/// }
/// </summary>
public record OrderApprovalRequestedEvent(
    [property: JsonPropertyName("eventId")] string EventId,
    [property: JsonPropertyName("eventType")] string EventType,
    [property: JsonPropertyName("correlationId")] string CorrelationId,
    [property: JsonPropertyName("occurredAt")] string OccurredAt,
    [property: JsonPropertyName("payload")] OrderApprovalRequestedPayload Payload
);

public record OrderApprovalRequestedPayload(
    [property: JsonPropertyName("orderId")] string OrderId,
    [property: JsonPropertyName("restaurantId")] string RestaurantId,
    [property: JsonPropertyName("userId")] string UserId,
    [property: JsonPropertyName("totalAmount")] MoneyDto TotalAmount,
    [property: JsonPropertyName("items")] List<OrderItemPayloadDto> Items
);

public record MoneyDto(
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("currency")] string Currency
);

public record OrderItemPayloadDto(
    [property: JsonPropertyName("menuItemId")] string MenuItemId,
    [property: JsonPropertyName("menuItemName")] string MenuItemName,
    [property: JsonPropertyName("quantity")] int Quantity,
    [property: JsonPropertyName("unitPrice")] decimal UnitPrice
);
