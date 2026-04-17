using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using RestaurantService.API.Consumers;
using RestaurantService.API.IntegrationEvents.Inbound;

namespace RestaurantService.Tests.Consumers;

public class OrderCompletedEventConsumerTests
{
    private readonly Mock<ILogger<OrderCompletedEventConsumer>> _loggerMock;
    private readonly OrderCompletedEventConsumer _sut;

    public OrderCompletedEventConsumerTests()
    {
        _loggerMock = new Mock<ILogger<OrderCompletedEventConsumer>>();
        _sut = new OrderCompletedEventConsumer(_loggerMock.Object);
    }

    [Fact]
    public async Task Consume_ShouldLogOrderCompletedInformation()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var restaurantId = Guid.NewGuid();
        var message = new OrderCompletedEvent(orderId, restaurantId, 250.50m, DateTime.UtcNow);

        var contextMock = new Mock<ConsumeContext<OrderCompletedEvent>>();
        contextMock.Setup(c => c.Message).Returns(message);

        // Act
        await _sut.Consume(contextMock.Object);

        // Assert — Consumer hata fırlatmamalı
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(restaurantId.ToString())),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Consume_ShouldCompleteSuccessfully()
    {
        // Arrange
        var message = new OrderCompletedEvent(Guid.NewGuid(), Guid.NewGuid(), 100m, DateTime.UtcNow);
        var contextMock = new Mock<ConsumeContext<OrderCompletedEvent>>();
        contextMock.Setup(c => c.Message).Returns(message);

        // Act
        var act = () => _sut.Consume(contextMock.Object);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
