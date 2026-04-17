using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RestaurantService.API.Controllers;
using RestaurantService.API.DTOs;
using RestaurantService.API.Services;

namespace RestaurantService.Tests.Controllers;

public class RestaurantsControllerTests
{
    private readonly Mock<IRestaurantService> _serviceMock;
    private readonly RestaurantsController _sut;

    public RestaurantsControllerTests()
    {
        _serviceMock = new Mock<IRestaurantService>();
        _sut = new RestaurantsController(_serviceMock.Object);
    }

    // ═════════════════════════════════════════════
    //  GetAll Tests
    // ═════════════════════════════════════════════

    [Fact]
    public async Task GetAll_ShouldReturnOkWithRestaurantList()
    {
        // Arrange
        var restaurants = new List<RestaurantSummaryDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Restaurant 1" },
            new() { Id = Guid.NewGuid(), Name = "Restaurant 2" }
        };
        _serviceMock.Setup(s => s.GetAllAsync(null, null, null, null))
            .ReturnsAsync(restaurants);

        // Act
        var result = await _sut.GetAll(null, null, null, null);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeAssignableTo<List<RestaurantSummaryDto>>().Subject;
        data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_WithFilters_ShouldPassFiltersToService()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetAllAsync("burger", "turkish", 41.0, 29.0))
            .ReturnsAsync(new List<RestaurantSummaryDto>());

        // Act
        await _sut.GetAll("burger", "turkish", 41.0, 29.0);

        // Assert
        _serviceMock.Verify(s => s.GetAllAsync("burger", "turkish", 41.0, 29.0), Times.Once);
    }

    // ═════════════════════════════════════════════
    //  GetById Tests
    // ═════════════════════════════════════════════

    [Fact]
    public async Task GetById_WithExistingId_ShouldReturnOkWithRestaurant()
    {
        // Arrange
        var id = Guid.NewGuid();
        var restaurant = new RestaurantDto { Id = id, Name = "Test" };
        _serviceMock.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(restaurant);

        // Act
        var result = await _sut.GetById(id);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeOfType<RestaurantDto>().Subject;
        data.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetById_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((RestaurantDto?)null);

        // Act
        var result = await _sut.GetById(Guid.NewGuid());

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // ═════════════════════════════════════════════
    //  GetNearby Tests
    // ═════════════════════════════════════════════

    [Fact]
    public async Task GetNearby_ShouldReturnOkWithNearbyRestaurants()
    {
        // Arrange
        var nearby = new List<RestaurantSummaryDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Nearby", DistanceKm = 1.5 }
        };
        _serviceMock.Setup(s => s.GetNearbyAsync(41.0, 29.0, 10))
            .ReturnsAsync(nearby);

        // Act
        var result = await _sut.GetNearby(41.0, 29.0, 10);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeAssignableTo<List<RestaurantSummaryDto>>().Subject;
        data.Should().HaveCount(1);
    }

    // ═════════════════════════════════════════════
    //  Create Tests
    // ═════════════════════════════════════════════

    [Fact]
    public async Task Create_ShouldReturnCreatedAtAction()
    {
        // Arrange
        var dto = new CreateRestaurantDto { Name = "Yeni Restaurant" };
        var created = new RestaurantDto { Id = Guid.NewGuid(), Name = "Yeni Restaurant" };
        _serviceMock.Setup(s => s.CreateAsync(dto)).ReturnsAsync(created);

        // Act
        var result = await _sut.Create(dto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(RestaurantsController.GetById));
        createdResult.Value.Should().BeOfType<RestaurantDto>();
    }

    // ═════════════════════════════════════════════
    //  Update Tests
    // ═════════════════════════════════════════════

    [Fact]
    public async Task Update_WithExistingId_ShouldReturnOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new UpdateRestaurantDto { Name = "Updated" };
        var updated = new RestaurantDto { Id = id, Name = "Updated" };
        _serviceMock.Setup(s => s.UpdateAsync(id, dto)).ReturnsAsync(updated);

        // Act
        var result = await _sut.Update(id, dto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeOfType<RestaurantDto>().Subject;
        data.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task Update_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange
        var dto = new UpdateRestaurantDto { Name = "Test" };
        _serviceMock.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), dto))
            .ReturnsAsync((RestaurantDto?)null);

        // Act
        var result = await _sut.Update(Guid.NewGuid(), dto);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // ═════════════════════════════════════════════
    //  UpdateStatus Tests
    // ═════════════════════════════════════════════

    [Fact]
    public async Task UpdateStatus_WithValidStatus_ShouldReturnOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new UpdateStatusDto { Status = "Closed" };
        var updated = new RestaurantDto { Id = id, Status = "Closed" };
        _serviceMock.Setup(s => s.UpdateStatusAsync(id, dto)).ReturnsAsync(updated);

        // Act
        var result = await _sut.UpdateStatus(id, dto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeOfType<RestaurantDto>().Subject;
        data.Status.Should().Be("Closed");
    }

    [Fact]
    public async Task UpdateStatus_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange
        var dto = new UpdateStatusDto { Status = "Open" };
        _serviceMock.Setup(s => s.UpdateStatusAsync(It.IsAny<Guid>(), dto))
            .ReturnsAsync((RestaurantDto?)null);

        // Act
        var result = await _sut.UpdateStatus(Guid.NewGuid(), dto);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateStatus_WithInvalidStatus_ShouldReturnBadRequest()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new UpdateStatusDto { Status = "Invalid" };
        _serviceMock.Setup(s => s.UpdateStatusAsync(id, dto))
            .ThrowsAsync(new ArgumentException("Invalid status"));

        // Act
        var result = await _sut.UpdateStatus(id, dto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ═════════════════════════════════════════════
    //  Delete Tests
    // ═════════════════════════════════════════════

    [Fact]
    public async Task Delete_WithExistingId_ShouldReturnNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.DeleteAsync(id)).ReturnsAsync(true);

        // Act
        var result = await _sut.Delete(id);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        // Act
        var result = await _sut.Delete(Guid.NewGuid());

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }
}
