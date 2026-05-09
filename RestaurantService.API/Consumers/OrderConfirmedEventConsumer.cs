using MassTransit;
using RestaurantService.API.IntegrationEvents.Inbound;
using RestaurantService.API.Services;

namespace RestaurantService.API.Consumers;

/// <summary>
/// order-service'den gelen "ORDER_CONFIRMED" event'ini dinler.
/// Ödeme alındı; stok rezervasyonları kalıcı olarak Confirmed durumuna çekilir.
/// Bu noktadan sonra OrderCancelled gelirse stok geri verilmez (gerçek hayatta
/// "iptal et + manuel iade" akışına gider). İstersek Confirmed → Released
/// genişlemesi de yapılabilir.
/// </summary>
public class OrderConfirmedEventConsumer : IConsumer<OrderConfirmedIntegrationEvent>
{
    private readonly IStockReservationService _reservationService;
    private readonly ILogger<OrderConfirmedEventConsumer> _logger;

    public OrderConfirmedEventConsumer(
        IStockReservationService reservationService,
        ILogger<OrderConfirmedEventConsumer> logger)
    {
        _reservationService = reservationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderConfirmedIntegrationEvent> context)
    {
        var msg = context.Message;
        var payload = msg?.Payload;

        if (payload is null || !Guid.TryParse(payload.OrderId, out var orderId))
        {
            _logger.LogWarning("Geçersiz OrderConfirmed payload: {Raw}", msg);
            return;
        }

        _logger.LogInformation("ORDER_CONFIRMED alındı. OrderId={OrderId}", orderId);

        await _reservationService.ConfirmAsync(orderId, context.CancellationToken);
    }
}
