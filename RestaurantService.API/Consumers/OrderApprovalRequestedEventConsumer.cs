using MassTransit;
using RestaurantService.API.IntegrationEvents;
using RestaurantService.API.IntegrationEvents.Inbound;
using RestaurantService.API.Services;

namespace RestaurantService.API.Consumers;

/// <summary>
/// order-service'den gelen "ORDER_RESTAURANT_APPROVAL_REQUESTED" event'ini dinler.
/// İş akışı:
///   1. Stok rezervasyonu dener (StockReservationService).
///   2. Başarılıysa  → OrderApprovedEvent + StockReservedEvent yayınlar.
///   3. Başarısızsa  → OrderRejectedEvent yayınlar (compensation tetikleyici).
///
/// NOT: Mevcut REST tabanlı /orders/restaurant/{id}/confirm akışı bozulmaz;
/// bu consumer onun async muadilidir, paralel çalışabilir.
/// </summary>
public class OrderApprovalRequestedEventConsumer : IConsumer<OrderApprovalRequestedEvent>
{
    private readonly IStockReservationService _reservationService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<OrderApprovalRequestedEventConsumer> _logger;

    private const int DefaultPrepMinutes = 25;

    public OrderApprovalRequestedEventConsumer(
        IStockReservationService reservationService,
        IPublishEndpoint publishEndpoint,
        ILogger<OrderApprovalRequestedEventConsumer> logger)
    {
        _reservationService = reservationService;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderApprovalRequestedEvent> context)
    {
        var msg = context.Message;
        var payload = msg?.Payload;

        if (payload is null
            || !Guid.TryParse(payload.OrderId, out var orderId)
            || !Guid.TryParse(payload.RestaurantId, out var restaurantId))
        {
            _logger.LogWarning("Geçersiz OrderApprovalRequested payload: {Raw}", msg);
            return;
        }

        _logger.LogInformation(
            "ORDER_RESTAURANT_APPROVAL_REQUESTED alındı. OrderId={OrderId} RestaurantId={RestaurantId} ItemCount={Count}",
            orderId, restaurantId, payload.Items?.Count ?? 0);

        var items = payload.Items ?? new List<OrderItemPayloadDto>();
        var result = await _reservationService.TryReserveAsync(
            orderId, restaurantId, items, context.CancellationToken);

        if (result.Success)
        {
            await _publishEndpoint.Publish(new OrderApprovedEvent(
                orderId,
                restaurantId,
                DefaultPrepMinutes,
                DateTime.UtcNow
            ), context.CancellationToken);

            await _publishEndpoint.Publish(new StockReservedEvent(
                orderId,
                restaurantId,
                result.ReservedItems.Select(i => new ReservedItemDto(i.ProductId, i.Quantity)).ToList(),
                DateTime.UtcNow
            ), context.CancellationToken);

            _logger.LogInformation("Order {OrderId} onaylandı ve stok rezerve edildi.", orderId);
        }
        else
        {
            await _publishEndpoint.Publish(new OrderRejectedEvent(
                orderId,
                restaurantId,
                result.FailureReason ?? "Bilinmeyen sebep",
                DateTime.UtcNow
            ), context.CancellationToken);

            _logger.LogWarning("Order {OrderId} REDDEDİLDİ: {Reason}", orderId, result.FailureReason);
        }
    }
}
