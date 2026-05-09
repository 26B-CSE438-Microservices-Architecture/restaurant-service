using System.Text.Json.Serialization;

namespace RestaurantService.API.IntegrationEvents.Inbound;

/// <summary>
/// order-service'den gelen "order.confirmed" event'i.
/// Ödeme alındı, sipariş kalıcılaştı → stok rezervasyonu Confirmed'e çekilir.
/// </summary>
public record OrderConfirmedIntegrationEvent(
    [property: JsonPropertyName("eventId")] string EventId,
    [property: JsonPropertyName("eventType")] string EventType,
    [property: JsonPropertyName("correlationId")] string CorrelationId,
    [property: JsonPropertyName("occurredAt")] string OccurredAt,
    [property: JsonPropertyName("payload")] OrderConfirmedPayload Payload
);

public record OrderConfirmedPayload(
    [property: JsonPropertyName("orderId")] string OrderId,
    [property: JsonPropertyName("userId")] string UserId,
    [property: JsonPropertyName("restaurantId")] string RestaurantId
);
