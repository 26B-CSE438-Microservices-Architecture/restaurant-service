using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RestaurantService.API.DTOs;

namespace RestaurantService.IntegrationTests;

/// <summary>
/// Menu API endpoint'lerinin tam HTTP pipeline üzerinden integration testleri.
/// Kategori ve ürün CRUD işlemlerini test eder.
/// </summary>
public class MenuApiTests : IClassFixture<MenuApiFactory>
{
    private readonly HttpClient _client;

    public MenuApiTests(MenuApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ─────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────
    private async Task<RestaurantDto> CreateRestaurantAsync()
    {
        var dto = new CreateRestaurantDto
        {
            Name = "Menu Test Restaurant",
            Latitude = 41.0,
            Longitude = 29.0
        };
        var response = await _client.PostAsJsonAsync("/api/v1/restaurants", dto);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RestaurantDto>())!;
    }

    private async Task<CategoryDto> CreateCategoryAsync(Guid restaurantId, string name = "Test Kategori")
    {
        var dto = new CreateCategoryDto { Name = name, DisplayOrder = 1 };
        var response = await _client.PostAsJsonAsync($"/api/v1/restaurants/{restaurantId}/categories", dto);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryDto>())!;
    }

    private async Task<ProductDto> CreateProductAsync(Guid categoryId, string name = "Test Ürün", decimal price = 50m)
    {
        var dto = new CreateProductDto
        {
            Name = name,
            Description = "Ürün açıklaması",
            Price = price,
            ImageUrl = "https://example.com/product.jpg"
        };
        var response = await _client.PostAsJsonAsync($"/api/v1/categories/{categoryId}/products", dto);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductDto>())!;
    }

    // ═════════════════════════════════════════════
    //  GET /api/v1/restaurants/{id}/menu
    // ═════════════════════════════════════════════

    [Fact]
    public async Task GetFullMenu_WithExistingRestaurant_ShouldReturn200()
    {
        // Arrange
        var restaurant = await CreateRestaurantAsync();
        var category = await CreateCategoryAsync(restaurant.Id, "Ana Yemekler");
        await CreateProductAsync(category.Id, "Kebap", 120m);

        // Act
        var response = await _client.GetAsync($"/api/v1/restaurants/{restaurant.Id}/menu");
        var menu = await response.Content.ReadFromJsonAsync<MenuDto>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        menu.Should().NotBeNull();
        menu!.RestaurantId.Should().Be(restaurant.Id);
        menu.Categories.Should().HaveCount(1);
        menu.Categories[0].Products.Should().HaveCount(1);
        menu.Categories[0].Products[0].Name.Should().Be("Kebap");
    }

    [Fact]
    public async Task GetFullMenu_WithNonExistingRestaurant_ShouldReturn404()
    {
        // Act
        var response = await _client.GetAsync($"/api/v1/restaurants/{Guid.NewGuid()}/menu");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ═════════════════════════════════════════════
    //  POST /api/v1/restaurants/{id}/categories
    // ═════════════════════════════════════════════

    [Fact]
    public async Task CreateCategory_ShouldReturn201()
    {
        // Arrange
        var restaurant = await CreateRestaurantAsync();
        var dto = new CreateCategoryDto { Name = "Tatlılar", DisplayOrder = 2 };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/restaurants/{restaurant.Id}/categories", dto);
        var created = await response.Content.ReadFromJsonAsync<CategoryDto>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        created.Should().NotBeNull();
        created!.Name.Should().Be("Tatlılar");
        created.DisplayOrder.Should().Be(2);
        created.RestaurantId.Should().Be(restaurant.Id);
    }

    [Fact]
    public async Task CreateCategory_WithNonExistingRestaurant_ShouldReturn404()
    {
        // Arrange
        var dto = new CreateCategoryDto { Name = "Test" };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/restaurants/{Guid.NewGuid()}/categories", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ═════════════════════════════════════════════
    //  PUT /api/v1/categories/{id}
    // ═════════════════════════════════════════════

    [Fact]
    public async Task UpdateCategory_ShouldReturn200()
    {
        // Arrange
        var restaurant = await CreateRestaurantAsync();
        var category = await CreateCategoryAsync(restaurant.Id, "Eski İsim");
        var updateDto = new UpdateCategoryDto { Name = "Yeni İsim", DisplayOrder = 5 };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/categories/{category.Id}", updateDto);
        var updated = await response.Content.ReadFromJsonAsync<CategoryDto>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        updated!.Name.Should().Be("Yeni İsim");
        updated.DisplayOrder.Should().Be(5);
    }

    [Fact]
    public async Task UpdateCategory_WithNonExistingId_ShouldReturn404()
    {
        // Arrange
        var dto = new UpdateCategoryDto { Name = "Test" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/categories/{Guid.NewGuid()}", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ═════════════════════════════════════════════
    //  DELETE /api/v1/categories/{id}
    // ═════════════════════════════════════════════

    [Fact]
    public async Task DeleteCategory_ShouldReturn204()
    {
        // Arrange
        var restaurant = await CreateRestaurantAsync();
        var category = await CreateCategoryAsync(restaurant.Id);

        // Act
        var response = await _client.DeleteAsync($"/api/v1/categories/{category.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteCategory_WithNonExistingId_ShouldReturn404()
    {
        // Act
        var response = await _client.DeleteAsync($"/api/v1/categories/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ═════════════════════════════════════════════
    //  POST /api/v1/categories/{id}/products
    // ═════════════════════════════════════════════

    [Fact]
    public async Task CreateProduct_ShouldReturn201()
    {
        // Arrange
        var restaurant = await CreateRestaurantAsync();
        var category = await CreateCategoryAsync(restaurant.Id);
        var dto = new CreateProductDto
        {
            Name = "Adana Kebap",
            Description = "Acılı",
            Price = 120m,
            ImageUrl = "https://img.com/adana.jpg"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/categories/{category.Id}/products", dto);
        var created = await response.Content.ReadFromJsonAsync<ProductDto>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        created.Should().NotBeNull();
        created!.Name.Should().Be("Adana Kebap");
        created.Price.Should().Be(120m);
        created.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task CreateProduct_WithNonExistingCategory_ShouldReturn404()
    {
        // Arrange
        var dto = new CreateProductDto { Name = "Test", Price = 10m };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/categories/{Guid.NewGuid()}/products", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ═════════════════════════════════════════════
    //  PUT /api/v1/products/{id}
    // ═════════════════════════════════════════════

    [Fact]
    public async Task UpdateProduct_ShouldReturn200()
    {
        // Arrange
        var restaurant = await CreateRestaurantAsync();
        var category = await CreateCategoryAsync(restaurant.Id);
        var product = await CreateProductAsync(category.Id, "Eski Ürün", 50m);
        var updateDto = new UpdateProductDto
        {
            Name = "Yeni Ürün",
            Description = "Yeni açıklama",
            Price = 75m,
            ImageUrl = "https://new.jpg"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/products/{product.Id}", updateDto);
        var updated = await response.Content.ReadFromJsonAsync<ProductDto>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        updated!.Name.Should().Be("Yeni Ürün");
        updated.Price.Should().Be(75m);
    }

    [Fact]
    public async Task UpdateProduct_WithNonExistingId_ShouldReturn404()
    {
        // Arrange
        var dto = new UpdateProductDto { Name = "Test", Price = 10m };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/products/{Guid.NewGuid()}", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ═════════════════════════════════════════════
    //  PATCH /api/v1/products/{id}/stock
    // ═════════════════════════════════════════════

    [Fact]
    public async Task ToggleProductStock_ShouldReturn200()
    {
        // Arrange
        var restaurant = await CreateRestaurantAsync();
        var category = await CreateCategoryAsync(restaurant.Id);
        var product = await CreateProductAsync(category.Id);
        var stockDto = new UpdateStockDto { IsAvailable = false };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/v1/products/{product.Id}/stock", stockDto);
        var updated = await response.Content.ReadFromJsonAsync<ProductDto>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        updated!.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleProductStock_WithNonExistingId_ShouldReturn404()
    {
        // Arrange
        var dto = new UpdateStockDto { IsAvailable = false };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/v1/products/{Guid.NewGuid()}/stock", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ═════════════════════════════════════════════
    //  DELETE /api/v1/products/{id}
    // ═════════════════════════════════════════════

    [Fact]
    public async Task DeleteProduct_ShouldReturn204()
    {
        // Arrange
        var restaurant = await CreateRestaurantAsync();
        var category = await CreateCategoryAsync(restaurant.Id);
        var product = await CreateProductAsync(category.Id);

        // Act
        var response = await _client.DeleteAsync($"/api/v1/products/{product.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteProduct_WithNonExistingId_ShouldReturn404()
    {
        // Act
        var response = await _client.DeleteAsync($"/api/v1/products/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ═════════════════════════════════════════════
    //  Full Menu Flow — End-to-End Senaryo
    // ═════════════════════════════════════════════

    [Fact]
    public async Task FullMenuFlow_ShouldWorkEndToEnd()
    {
        // 1. Restaurant oluştur
        var restaurant = await CreateRestaurantAsync();

        // 2. Kategoriler oluştur
        var anaYemekler = await CreateCategoryAsync(restaurant.Id, "Ana Yemekler");
        var icecekler = await CreateCategoryAsync(restaurant.Id, "İçecekler");

        // 3. Ürünler ekle
        var kebap = await CreateProductAsync(anaYemekler.Id, "Adana Kebap", 120m);
        var lahmacun = await CreateProductAsync(anaYemekler.Id, "Lahmacun", 45m);
        var ayran = await CreateProductAsync(icecekler.Id, "Ayran", 15m);

        // 4. Full menüyü al ve kontrol et
        var menuResponse = await _client.GetAsync($"/api/v1/restaurants/{restaurant.Id}/menu");
        var menu = await menuResponse.Content.ReadFromJsonAsync<MenuDto>();

        menu.Should().NotBeNull();
        menu!.Categories.Should().HaveCount(2);
        menu.Categories.SelectMany(c => c.Products).Should().HaveCount(3);

        // 5. Ürün stoktan kaldır
        var stockDto = new UpdateStockDto { IsAvailable = false };
        var stockResponse = await _client.PatchAsJsonAsync($"/api/v1/products/{kebap.Id}/stock", stockDto);
        stockResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 6. Ürün sil
        var deleteResponse = await _client.DeleteAsync($"/api/v1/products/{lahmacun.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 7. Menüyü tekrar kontrol et — 2 ürün kalmalı
        var menuResponse2 = await _client.GetAsync($"/api/v1/restaurants/{restaurant.Id}/menu");
        var menu2 = await menuResponse2.Content.ReadFromJsonAsync<MenuDto>();
        menu2!.Categories.SelectMany(c => c.Products).Should().HaveCount(2);
    }
}
