using System.Text.Json.Serialization;

namespace RestaurantService.API.IntegrationEvents.Inbound;

/// <summary>
/// order-service'den gelen "order.cancelled" event'i.
/// Hatalı işlem sonrası compensation (stok geri açma) tetikleyicisidir.
/// </summary>
public record OrderCancelledIntegrationEvent(
    [property: JsonPropertyName("eventId")] string EventId,
    [property: JsonPropertyName("eventType")] string EventType,
    [property: JsonPropertyName("correlationId")] string CorrelationId,
    [property: JsonPropertyName("occurredAt")] string OccurredAt,
    [property: JsonPropertyName("payload")] OrderCancelledPayload Payload
);

public record OrderCancelledPayload(
    [property: JsonPropertyName("orderId")] string OrderId,
    [property: JsonPropertyName("userId")] string UserId,
    [property: JsonPropertyName("cancelledBy")] string CancelledBy,
    [property: JsonPropertyName("refundRequired")] bool RefundRequired,
    [property: JsonPropertyName("paymentId")] string PaymentId
);
