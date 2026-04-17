using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using RestaurantService.API.Consumers;
using RestaurantService.API.IntegrationEvents.Inbound;

namespace RestaurantService.Tests.Consumers;

public class UserRoleChangedEventConsumerTests
{
    private readonly Mock<ILogger<UserRoleChangedEventConsumer>> _loggerMock;
    private readonly UserRoleChangedEventConsumer _sut;

    public UserRoleChangedEventConsumerTests()
    {
        _loggerMock = new Mock<ILogger<UserRoleChangedEventConsumer>>();
        _sut = new UserRoleChangedEventConsumer(_loggerMock.Object);
    }

    [Fact]
    public async Task Consume_ShouldLogRoleChangeInformation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var message = new UserRoleChangedEvent(userId, "RestaurantOwner", DateTime.UtcNow);

        var contextMock = new Mock<ConsumeContext<UserRoleChangedEvent>>();
        contextMock.Setup(c => c.Message).Returns(message);

        // Act
        await _sut.Consume(contextMock.Object);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(userId.ToString())),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Consume_ShouldCompleteSuccessfully()
    {
        // Arrange
        var message = new UserRoleChangedEvent(Guid.NewGuid(), "Admin", DateTime.UtcNow);
        var contextMock = new Mock<ConsumeContext<UserRoleChangedEvent>>();
        contextMock.Setup(c => c.Message).Returns(message);

        // Act
        var act = () => _sut.Consume(contextMock.Object);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Consume_WithDifferentRoles_ShouldHandleAll()
    {
        // Arrange & Act & Assert — Farklı roller ile test
        var roles = new[] { "Customer", "RestaurantOwner", "Admin", "Courier" };

        foreach (var role in roles)
        {
            var message = new UserRoleChangedEvent(Guid.NewGuid(), role, DateTime.UtcNow);
            var contextMock = new Mock<ConsumeContext<UserRoleChangedEvent>>();
            contextMock.Setup(c => c.Message).Returns(message);

            var act = () => _sut.Consume(contextMock.Object);
            await act.Should().NotThrowAsync($"Role '{role}' should be handled without error");
        }
    }
}
