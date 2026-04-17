using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RestaurantService.API.DTOs;

namespace RestaurantService.IntegrationTests;

/// <summary>
/// Restaurant API endpoint'lerinin tam HTTP pipeline üzerinden integration testleri.
/// HTTP Request → Routing → Controller → Service → Database → Response
/// </summary>
public class RestaurantApiTests : IClassFixture<RestaurantApiFactory>
{
    private readonly HttpClient _client;

    public RestaurantApiTests(RestaurantApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ─────────────────────────────────────────────
    // Helper: Yeni restoran oluşturur ve DTO döner
    // ─────────────────────────────────────────────
    private async Task<RestaurantDto> CreateTestRestaurantAsync(string name = "Integration Test Restaurant")
    {
        var dto = new CreateRestaurantDto
        {
            Name = name,
            Description = "Integration test açıklaması",
            AddressText = "Test Caddesi No:1",
            Latitude = 41.0082,
            Longitude = 28.9784,
            LogoUrl = "https://example.com/logo.png",
            MinOrderAmount = 50m,
            DeliveryFee = 10m,
            OpeningTime = new TimeOnly(9, 0),
            ClosingTime = new TimeOnly(23, 0)
        };

        var response = await _client.PostAsJsonAsync("/api/v1/restaurants", dto);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RestaurantDto>())!;
    }

    // ═════════════════════════════════════════════
    //  POST /api/v1/restaurants
    // ═════════════════════════════════════════════

    [Fact]
    public async Task CreateRestaurant_ShouldReturn201Created()
    {
        // Arrange
        var dto = new CreateRestaurantDto
        {
            Name = "Yeni Restaurant",
            Description = "Harika lezzetler",
            AddressText = "İstanbul",
            Latitude = 41.0,
            Longitude = 29.0,
            MinOrderAmount = 30m,
            DeliveryFee = 5m,
            OpeningTime = new TimeOnly(8, 0),
            ClosingTime = new TimeOnly(22, 0)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/restaurants", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<RestaurantDto>();
        created.Should().NotBeNull();
        created!.Name.Should().Be("Yeni Restaurant");
        created.IsActive.Should().BeTrue();
        created.Status.Should().Be("Open");
        created.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateRestaurant_ShouldReturnLocationHeader()
    {
        // Arrange
        var dto = new CreateRestaurantDto
        {
            Name = "Location Header Test",
            Latitude = 41.0,
            Longitude = 29.0
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/restaurants", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    // ═════════════════════════════════════════════
    //  GET /api/v1/restaurants
    // ═════════════════════════════════════════════

    [Fact]
    public async Task GetAllRestaurants_ShouldReturn200()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/restaurants");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllRestaurants_AfterCreate_ShouldContainCreatedRestaurant()
    {
        // Arrange
        var created = await CreateTestRestaurantAsync("Search Test Restaurant");

        // Act
        var response = await _client.GetAsync("/api/v1/restaurants");
        var restaurants = await response.Content.ReadFromJsonAsync<List<RestaurantSummaryDto>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        restaurants.Should().NotBeNull();
        restaurants.Should().Contain(r => r.Name == "Search Test Restaurant");
    }

    [Fact]
    public async Task GetAllRestaurants_WithNameFilter_ShouldFilterResults()
    {
        // Arrange
        await CreateTestRestaurantAsync("Unique Burger Place");
        await CreateTestRestaurantAsync("Pizza House");

        // Act
        var response = await _client.GetAsync("/api/v1/restaurants?name=Unique Burger");
        var restaurants = await response.Content.ReadFromJsonAsync<List<RestaurantSummaryDto>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        restaurants.Should().NotBeNull();
        restaurants.Should().OnlyContain(r => r.Name.Contains("Unique Burger"));
    }

    // ═════════════════════════════════════════════
    //  GET /api/v1/restaurants/{id}
    // ═════════════════════════════════════════════

    [Fact]
    public async Task GetRestaurantById_WithExistingId_ShouldReturn200()
    {
        // Arrange
        var created = await CreateTestRestaurantAsync();

        // Act
        var response = await _client.GetAsync($"/api/v1/restaurants/{created.Id}");
        var restaurant = await response.Content.ReadFromJsonAsync<RestaurantDto>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        restaurant.Should().NotBeNull();
        restaurant!.Id.Should().Be(created.Id);
        restaurant.Name.Should().Be(created.Name);
    }

    [Fact]
    public async Task GetRestaurantById_WithNonExistingId_ShouldReturn404()
    {
        // Act
        var response = await _client.GetAsync($"/api/v1/restaurants/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ═════════════════════════════════════════════
    //  GET /api/v1/restaurants/nearby
    // ═════════════════════════════════════════════

    [Fact]
    public async Task GetNearbyRestaurants_ShouldReturn200()
    {
        // Arrange
        await CreateTestRestaurantAsync();

        // Act
        var response = await _client.GetAsync("/api/v1/restaurants/nearby?lat=41.0&lng=29.0&radiusKm=50");
        var restaurants = await response.Content.ReadFromJsonAsync<List<RestaurantSummaryDto>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        restaurants.Should().NotBeNull();
    }

    // ═════════════════════════════════════════════
    //  PUT /api/v1/restaurants/{id}
    // ═════════════════════════════════════════════

    [Fact]
    public async Task UpdateRestaurant_WithExistingId_ShouldReturn200()
    {
        // Arrange
        var created = await CreateTestRestaurantAsync();
        var updateDto = new UpdateRestaurantDto
        {
            Name = "Updated Restaurant Name",
            Description = "Updated Description",
            AddressText = "Updated Address",
            Latitude = 40.0,
            Longitude = 30.0,
            MinOrderAmount = 100m,
            DeliveryFee = 15m,
            OpeningTime = new TimeOnly(10, 0),
            ClosingTime = new TimeOnly(23, 0)
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/restaurants/{created.Id}", updateDto);
        var updated = await response.Content.ReadFromJsonAsync<RestaurantDto>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated Restaurant Name");
        updated.MinOrderAmount.Should().Be(100m);
    }

    [Fact]
    public async Task UpdateRestaurant_WithNonExistingId_ShouldReturn404()
    {
        // Arrange
        var updateDto = new UpdateRestaurantDto { Name = "Test", Latitude = 41.0, Longitude = 29.0 };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/restaurants/{Guid.NewGuid()}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ═════════════════════════════════════════════
    //  PATCH /api/v1/restaurants/{id}/status
    // ═════════════════════════════════════════════

    [Fact]
    public async Task UpdateStatus_ToClose_ShouldReturn200()
    {
        // Arrange
        var created = await CreateTestRestaurantAsync();
        var statusDto = new UpdateStatusDto { Status = "Closed" };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/v1/restaurants/{created.Id}/status", statusDto);
        var updated = await response.Content.ReadFromJsonAsync<RestaurantDto>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        updated!.Status.Should().Be("Closed");
    }

    [Fact]
    public async Task UpdateStatus_WithInvalidStatus_ShouldReturn400()
    {
        // Arrange
        var created = await CreateTestRestaurantAsync();
        var statusDto = new UpdateStatusDto { Status = "InvalidStatus" };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/v1/restaurants/{created.Id}/status", statusDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateStatus_WithNonExistingId_ShouldReturn404()
    {
        // Arrange
        var statusDto = new UpdateStatusDto { Status = "Open" };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/v1/restaurants/{Guid.NewGuid()}/status", statusDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ═════════════════════════════════════════════
    //  DELETE /api/v1/restaurants/{id}
    // ═════════════════════════════════════════════

    [Fact]
    public async Task DeleteRestaurant_WithExistingId_ShouldReturn204()
    {
        // Arrange
        var created = await CreateTestRestaurantAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/v1/restaurants/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteRestaurant_ThenGetById_ShouldReturn404()
    {
        // Arrange
        var created = await CreateTestRestaurantAsync();
        await _client.DeleteAsync($"/api/v1/restaurants/{created.Id}");

        // Act
        var response = await _client.GetAsync($"/api/v1/restaurants/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteRestaurant_WithNonExistingId_ShouldReturn404()
    {
        // Act
        var response = await _client.DeleteAsync($"/api/v1/restaurants/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ═════════════════════════════════════════════
    //  Full CRUD Flow — End-to-End Senaryo
    // ═════════════════════════════════════════════

    [Fact]
    public async Task FullCrudFlow_CreateReadUpdateDelete_ShouldWorkEndToEnd()
    {
        // 1. CREATE
        var createDto = new CreateRestaurantDto
        {
            Name = "E2E Test Restaurant",
            Description = "End to end test",
            AddressText = "E2E Street",
            Latitude = 41.0,
            Longitude = 29.0,
            MinOrderAmount = 25m,
            DeliveryFee = 5m,
            OpeningTime = new TimeOnly(8, 0),
            ClosingTime = new TimeOnly(22, 0)
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/restaurants", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<RestaurantDto>();

        // 2. READ
        var getResponse = await _client.GetAsync($"/api/v1/restaurants/{created!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<RestaurantDto>();
        fetched!.Name.Should().Be("E2E Test Restaurant");

        // 3. UPDATE
        var updateDto = new UpdateRestaurantDto
        {
            Name = "E2E Updated",
            Description = "Updated",
            AddressText = "Updated Street",
            Latitude = 40.0,
            Longitude = 30.0,
            MinOrderAmount = 50m,
            DeliveryFee = 10m,
            OpeningTime = new TimeOnly(9, 0),
            ClosingTime = new TimeOnly(23, 0)
        };
        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/restaurants/{created.Id}", updateDto);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<RestaurantDto>();
        updated!.Name.Should().Be("E2E Updated");

        // 4. DELETE
        var deleteResponse = await _client.DeleteAsync($"/api/v1/restaurants/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 5. READ after DELETE — should be gone
        var afterDelete = await _client.GetAsync($"/api/v1/restaurants/{created.Id}");
        afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
