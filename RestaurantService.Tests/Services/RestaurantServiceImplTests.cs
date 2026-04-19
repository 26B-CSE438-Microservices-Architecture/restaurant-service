using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Moq;
using RestaurantService.API.Data;
using RestaurantService.API.DTOs;
using RestaurantService.API.Entities;
using RestaurantService.API.IntegrationEvents;
using RestaurantService.API.Services;
using RestaurantService.Tests.Helpers;

namespace RestaurantService.Tests.Services;

public class RestaurantServiceImplTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly RestaurantServiceImpl _sut; // System Under Test

    public RestaurantServiceImplTests()
    {
        _db = TestDbContextFactory.Create();
        _publishEndpointMock = new Mock<IPublishEndpoint>();
        _sut = new RestaurantServiceImpl(_db, _publishEndpointMock.Object);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    // ─────────────────────────────────────────────
    // Helper: Veritabanına test restoranı ekler
    // ─────────────────────────────────────────────
    private async Task<Restaurant> SeedRestaurantAsync(
        string name = "Test Restaurant",
        bool isActive = true,
        RestaurantStatus status = RestaurantStatus.Open,
        double lat = 41.0082,
        double lng = 28.9784,
        string description = "Test Description",
        string cuisineType = "Test Cuisine")
    {
        var restaurant = new Restaurant
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            CuisineType = cuisineType,
            AddressText = "Test Address",
            Latitude = lat,
            Longitude = lng,
            LogoUrl = "https://example.com/logo.png",
            MinOrderAmount = 50m,
            DeliveryFee = 10m,
            IsActive = isActive,
            Status = status,
            OpeningTime = new TimeOnly(9, 0),
            ClosingTime = new TimeOnly(23, 0),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Restaurants.Add(restaurant);
        await _db.SaveChangesAsync();
        return restaurant;
    }

    // ═════════════════════════════════════════════
    //  GetAllAsync Tests
    // ═════════════════════════════════════════════

    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyActiveRestaurants()
    {
        // Arrange
        await SeedRestaurantAsync("Active Restaurant", isActive: true);
        await SeedRestaurantAsync("Inactive Restaurant", isActive: false);

        // Act
        var result = await _sut.GetAllAsync(null, null, null, null);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Active Restaurant");
    }

    [Fact]
    public async Task GetAllAsync_WithNameFilter_ShouldFilterByName()
    {
        // Arrange
        await SeedRestaurantAsync("Burger King");
        await SeedRestaurantAsync("Pizza Hut");
        await SeedRestaurantAsync("Burger Lab");

        // Act
        var result = await _sut.GetAllAsync("burger", null, null, null);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(r => r.Name.ToLower().Should().Contain("burger"));
    }

    [Fact]
    public async Task GetAllAsync_WithCuisineFilter_ShouldFilterByCuisineType()
    {
        // Arrange
        await SeedRestaurantAsync("Restaurant A", cuisineType: "Italian");
        await SeedRestaurantAsync("Restaurant B", cuisineType: "Turkish Kebab");

        // Sanity check: veritabanında cuisine type doğru kaydedilmiş mi?
        var allInDb = await _db.Restaurants.ToListAsync();
        allInDb.Should().HaveCount(2);
        allInDb.Should().Contain(r => r.CuisineType.Contains("Italian"));

        // Act
        var result = await _sut.GetAllAsync(null, "Italian", null, null);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Restaurant A");
    }

    [Fact]
    public async Task GetAllAsync_WithCoordinates_ShouldCalculateDistanceAndSort()
    {
        // Arrange - İstanbul ve Ankara
        await SeedRestaurantAsync("İstanbul Restaurantı", lat: 41.0082, lng: 28.9784);
        await SeedRestaurantAsync("Ankara Restaurantı", lat: 39.9334, lng: 32.8597);

        // Act - Kullanıcı İstanbul'da
        var result = await _sut.GetAllAsync(null, null, 41.0, 29.0);

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("İstanbul Restaurantı");
        result[0].DistanceKm.Should().NotBeNull();
        result[0].DistanceKm.Should().BeLessThan(result[1].DistanceKm!.Value);
    }

    [Fact]
    public async Task GetAllAsync_WithNoRestaurants_ShouldReturnEmptyList()
    {
        // Act
        var result = await _sut.GetAllAsync(null, null, null, null);

        // Assert
        result.Should().BeEmpty();
    }

    // ═════════════════════════════════════════════
    //  GetByIdAsync Tests
    // ═════════════════════════════════════════════

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnRestaurant()
    {
        // Arrange
        var restaurant = await SeedRestaurantAsync("Test Restaurant");

        // Act
        var result = await _sut.GetByIdAsync(restaurant.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(restaurant.Id);
        result.Name.Should().Be("Test Restaurant");
        result.Status.Should().Be("Open");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Act
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    // ═════════════════════════════════════════════
    //  GetNearbyAsync Tests
    // ═════════════════════════════════════════════

    [Fact]
    public async Task GetNearbyAsync_ShouldReturnOnlyRestaurantsWithinRadius()
    {
        // Arrange
        await SeedRestaurantAsync("Yakın Restaurant", lat: 41.01, lng: 28.98);   // ~1km
        await SeedRestaurantAsync("Uzak Restaurant", lat: 39.93, lng: 32.86);    // ~350km

        // Act - 5km radius
        var result = await _sut.GetNearbyAsync(41.0082, 28.9784, 5);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Yakın Restaurant");
        result[0].DistanceKm.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task GetNearbyAsync_ShouldOnlyReturnActiveRestaurants()
    {
        // Arrange
        await SeedRestaurantAsync("Active Nearby", isActive: true, lat: 41.01, lng: 28.98);
        await SeedRestaurantAsync("Inactive Nearby", isActive: false, lat: 41.01, lng: 28.98);

        // Act
        var result = await _sut.GetNearbyAsync(41.0082, 28.9784, 5);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Active Nearby");
    }

    [Fact]
    public async Task GetNearbyAsync_ShouldReturnSortedByDistance()
    {
        // Arrange
        await SeedRestaurantAsync("Orta Mesafe", lat: 41.05, lng: 29.05);
        await SeedRestaurantAsync("En Yakın", lat: 41.009, lng: 28.979);
        await SeedRestaurantAsync("Biraz Uzak", lat: 41.1, lng: 29.1);

        // Act
        var result = await _sut.GetNearbyAsync(41.0082, 28.9784, 50);

        // Assert
        result.Should().HaveCount(3);
        result[0].DistanceKm.Should().BeLessThanOrEqualTo(result[1].DistanceKm!.Value);
        result[1].DistanceKm.Should().BeLessThanOrEqualTo(result[2].DistanceKm!.Value);
    }

    // ═════════════════════════════════════════════
    //  CreateAsync Tests
    // ═════════════════════════════════════════════

    [Fact]
    public async Task CreateAsync_ShouldCreateRestaurantAndReturnDto()
    {
        // Arrange
        var dto = new CreateRestaurantDto
        {
            Name = "Yeni Restaurant",
            Description = "Açıklama",
            AddressText = "Adres",
            Latitude = 41.0,
            Longitude = 29.0,
            LogoUrl = "https://logo.png",
            MinOrderAmount = 30m,
            DeliveryFee = 5m,
            OpeningTime = new TimeOnly(8, 0),
            ClosingTime = new TimeOnly(22, 0)
        };

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Yeni Restaurant");
        result.IsActive.Should().BeTrue();
        result.Status.Should().Be("Open");
        result.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateAsync_ShouldSaveToDatabase()
    {
        // Arrange
        var dto = new CreateRestaurantDto
        {
            Name = "DB Test Restaurant",
            Latitude = 41.0,
            Longitude = 29.0
        };

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        var dbRestaurant = await _db.Restaurants.FindAsync(result.Id);
        dbRestaurant.Should().NotBeNull();
        dbRestaurant!.Name.Should().Be("DB Test Restaurant");
    }

    [Fact]
    public async Task CreateAsync_ShouldPublishRestaurantCreatedEvent()
    {
        // Arrange
        var dto = new CreateRestaurantDto
        {
            Name = "Event Test",
            Latitude = 41.0,
            Longitude = 29.0
        };

        // Act
        await _sut.CreateAsync(dto);

        // Assert
        _publishEndpointMock.Verify(
            x => x.Publish(
                It.Is<RestaurantCreatedEvent>(e =>
                    e.Name == "Event Test" &&
                    e.Latitude == 41.0 &&
                    e.Longitude == 29.0),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═════════════════════════════════════════════
    //  UpdateAsync Tests
    // ═════════════════════════════════════════════

    [Fact]
    public async Task UpdateAsync_WithExistingId_ShouldUpdateAndReturnDto()
    {
        // Arrange
        var restaurant = await SeedRestaurantAsync("Eski İsim");
        var dto = new UpdateRestaurantDto
        {
            Name = "Yeni İsim",
            Description = "Yeni Açıklama",
            AddressText = "Yeni Adres",
            Latitude = 40.0,
            Longitude = 30.0,
            LogoUrl = "https://newlogo.png",
            MinOrderAmount = 100m,
            DeliveryFee = 15m,
            OpeningTime = new TimeOnly(10, 0),
            ClosingTime = new TimeOnly(23, 0)
        };

        // Act
        var result = await _sut.UpdateAsync(restaurant.Id, dto);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Yeni İsim");
        result.Description.Should().Be("Yeni Açıklama");
        result.MinOrderAmount.Should().Be(100m);
        result.DeliveryFee.Should().Be(15m);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        var dto = new UpdateRestaurantDto { Name = "Test" };

        // Act
        var result = await _sut.UpdateAsync(Guid.NewGuid(), dto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldPublishRestaurantUpdatedEvent()
    {
        // Arrange
        var restaurant = await SeedRestaurantAsync();
        var dto = new UpdateRestaurantDto
        {
            Name = "Updated",
            Latitude = 41.0,
            Longitude = 29.0
        };

        // Act
        await _sut.UpdateAsync(restaurant.Id, dto);

        // Assert
        _publishEndpointMock.Verify(
            x => x.Publish(
                It.Is<RestaurantUpdatedEvent>(e => e.RestaurantId == restaurant.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═════════════════════════════════════════════
    //  UpdateStatusAsync Tests
    // ═════════════════════════════════════════════

    [Fact]
    public async Task UpdateStatusAsync_WithValidStatus_ShouldUpdateStatus()
    {
        // Arrange
        var restaurant = await SeedRestaurantAsync(status: RestaurantStatus.Open);
        var dto = new UpdateStatusDto { Status = "Closed" };

        // Act
        var result = await _sut.UpdateStatusAsync(restaurant.Id, dto);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be("Closed");
    }

    [Fact]
    public async Task UpdateStatusAsync_WithBusyStatus_ShouldWork()
    {
        // Arrange
        var restaurant = await SeedRestaurantAsync(status: RestaurantStatus.Open);
        var dto = new UpdateStatusDto { Status = "Busy" };

        // Act
        var result = await _sut.UpdateStatusAsync(restaurant.Id, dto);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be("Busy");
    }

    [Fact]
    public async Task UpdateStatusAsync_WithInvalidStatus_ShouldThrowArgumentException()
    {
        // Arrange
        var restaurant = await SeedRestaurantAsync();
        var dto = new UpdateStatusDto { Status = "InvalidStatus" };

        // Act
        var act = () => _sut.UpdateStatusAsync(restaurant.Id, dto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Invalid status*");
    }

    [Fact]
    public async Task UpdateStatusAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        var dto = new UpdateStatusDto { Status = "Open" };

        // Act
        var result = await _sut.UpdateStatusAsync(Guid.NewGuid(), dto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldPublishStatusChangedEvent()
    {
        // Arrange
        var restaurant = await SeedRestaurantAsync(status: RestaurantStatus.Open);
        var dto = new UpdateStatusDto { Status = "Closed" };

        // Act
        await _sut.UpdateStatusAsync(restaurant.Id, dto);

        // Assert
        _publishEndpointMock.Verify(
            x => x.Publish(
                It.Is<RestaurantStatusChangedEvent>(e =>
                    e.RestaurantId == restaurant.Id &&
                    e.OldStatus == "Open" &&
                    e.NewStatus == "Closed"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═════════════════════════════════════════════
    //  DeleteAsync Tests
    // ═════════════════════════════════════════════

    [Fact]
    public async Task DeleteAsync_WithExistingId_ShouldDeleteAndReturnTrue()
    {
        // Arrange
        var restaurant = await SeedRestaurantAsync();

        // Act
        var result = await _sut.DeleteAsync(restaurant.Id);

        // Assert
        result.Should().BeTrue();
        var dbRestaurant = await _db.Restaurants.FindAsync(restaurant.Id);
        dbRestaurant.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingId_ShouldReturnFalse()
    {
        // Act
        var result = await _sut.DeleteAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ShouldPublishRestaurantDeletedEvent()
    {
        // Arrange
        var restaurant = await SeedRestaurantAsync();

        // Act
        await _sut.DeleteAsync(restaurant.Id);

        // Assert
        _publishEndpointMock.Verify(
            x => x.Publish(
                It.Is<RestaurantDeletedEvent>(e => e.RestaurantId == restaurant.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═════════════════════════════════════════════
    //  Mapping Tests
    // ═════════════════════════════════════════════

    [Fact]
    public async Task GetByIdAsync_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var restaurant = await SeedRestaurantAsync("Mapping Test");

        // Act
        var result = await _sut.GetByIdAsync(restaurant.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(restaurant.Id);
        result.Name.Should().Be(restaurant.Name);
        result.Description.Should().Be(restaurant.Description);
        result.AddressText.Should().Be(restaurant.AddressText);
        result.Latitude.Should().Be(restaurant.Latitude);
        result.Longitude.Should().Be(restaurant.Longitude);
        result.LogoUrl.Should().Be(restaurant.LogoUrl);
        result.MinOrderAmount.Should().Be(restaurant.MinOrderAmount);
        result.DeliveryFee.Should().Be(restaurant.DeliveryFee);
        result.IsActive.Should().Be(restaurant.IsActive);
        result.Status.Should().Be(restaurant.Status.ToString());
    }
}
