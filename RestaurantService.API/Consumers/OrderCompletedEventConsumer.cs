using MassTransit;
using Microsoft.Extensions.Logging;
using RestaurantService.API.IntegrationEvents.Inbound;
using System.Threading.Tasks;

namespace RestaurantService.API.Consumers;

public class OrderCompletedEventConsumer : IConsumer<OrderCompletedEvent>
{
    private readonly ILogger<OrderCompletedEventConsumer> _logger;

    public OrderCompletedEventConsumer(ILogger<OrderCompletedEventConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<OrderCompletedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Restoran {RestaurantId} için sipariş (ID: {OrderId}) tamamlandı. Tutar: {Amount}. Zaman: {Timestamp}",
            message.RestaurantId, message.OrderId, message.TotalAmount, message.Timestamp);

        // İleride burada istatistik güncelleme işlemi veya veritabanı loglama yapılabilir.
        return Task.CompletedTask;
    }
}
