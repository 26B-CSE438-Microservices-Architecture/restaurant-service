using MassTransit;
using Microsoft.Extensions.Logging;
using RestaurantService.API.IntegrationEvents.Inbound;
using System.Threading.Tasks;

namespace RestaurantService.API.Consumers;

public class UserRoleChangedEventConsumer : IConsumer<UserRoleChangedEvent>
{
    private readonly ILogger<UserRoleChangedEventConsumer> _logger;

    public UserRoleChangedEventConsumer(ILogger<UserRoleChangedEventConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<UserRoleChangedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Kullanıcı {UserId} rolü {NewRole} olarak güncellendi. Zaman: {Timestamp}",
            message.UserId, message.NewRole, message.Timestamp);

        // İleride yetkilendirme / atama tarafında güncelleme yapılabilir.
        return Task.CompletedTask;
    }
}
