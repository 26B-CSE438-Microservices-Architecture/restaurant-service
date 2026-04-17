using FluentAssertions;
using MassTransit;
using Moq;
using RestaurantService.API.Data;
using RestaurantService.API.DTOs;
using RestaurantService.API.Entities;
using RestaurantService.API.IntegrationEvents;
using RestaurantService.API.Services;
using RestaurantService.Tests.Helpers;

namespace RestaurantService.Tests.Services;

public class MenuServiceImplTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly MenuServiceImpl _sut;

    public MenuServiceImplTests()
    {
        _db = TestDbContextFactory.Create();
        _publishEndpointMock = new Mock<IPublishEndpoint>();
        _sut = new MenuServiceImpl(_db, _publishEndpointMock.Object);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    // ─────────────────────────────────────────────
    // Helper: Test verileri oluşturur
    // ─────────────────────────────────────────────
    private async Task<Restaurant> SeedRestaurantAsync(string name = "Test Restaurant")
    {
        var restaurant = new Restaurant
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "Test",
            IsActive = true,
            Status = RestaurantStatus.Open,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Restaurants.Add(restaurant);
        await _db.SaveChangesAsync();
        return restaurant;
    }

    private async Task<MenuCategory> SeedCategoryAsync(Guid restaurantId, string name = "Test Category", int order = 0)
    {
        var category = new MenuCategory
        {
            Id = Guid.NewGuid(),
            RestaurantId = restaurantId,
            Name = name,
            DisplayOrder = order
        };
        _db.MenuCategories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    private async Task<Product> SeedProductAsync(Guid categoryId, string name = "Test Product", decimal price = 25m)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = categoryId,
            Name = name,
            Description = "Ürün açıklaması",
            Price = price,
            IsAvailable = true,
            ImageUrl = "https://example.com/image.png"
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return product;
    }

    // ═════════════════════════════════════════════
    //  GetFullMenuAsync Tests
    // ═════════════════════════════════════════════

    [Fact]
    public async Task GetFullMenuAsync_WithExistingRestaurant_ShouldReturnFullMenu()
    {
        // Arrange
        var restaurant = await SeedRestaurantAsync("Menü Restoranı");
        var category = await SeedCategoryAsync(restaurant.Id, "Ana Yemekler", 1);
        await SeedProductAsync(category.Id, "Adana Kebap", 120m);
        await SeedProductAsync(category.Id, "Lahmacun", 45m);

        // Act
        var result = await _sut.GetFullMenuAsync(restaurant.Id);

        // Assert
        result.Should().NotBeNull();
        result!.RestaurantId.Should().Be(restaurant.Id);
        result.RestaurantName.Should().Be("Menü Restoranı");
        result.Categories.Should().HaveCount(1);
        result.Categories[0].Products.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetFullMenuAsync_WithNonExistingRestaurant_ShouldReturnNull()
    {
        // Act
        var result = await _sut.GetFullMenuAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetFullMenuAsync_ShouldReturnAllCategoriesWithCorrectDisplayOrder()
    {
        // Arrange
        var restaurant = await SeedRestaurantAsync();
        await SeedCategoryAsync(restaurant.Id, "Tatlılar", 3);
        await SeedCategoryAsync(restaurant.Id, "Ana Yemekler", 1);
        await SeedCategoryAsync(restaurant.Id, "İçecekler", 2);

        // Act
        var result = await _sut.GetFullMenuAsync(restaurant.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Categories.Should().HaveCount(3);

        // Tüm kategorilerin doğru DisplayOrder değerleri ile döndüğünü kontrol et
        var categoryNames = result.Categories.Select(c => c.Name).ToList();
        categoryNames.Should().Contain("Ana Yemekler");
        categoryNames.Should().Contain("İçecekler");
        categoryNames.Should().Contain("Tatlılar");

        result.Categories.Should().Contain(c => c.DisplayOrder == 1);
        result.Categories.Should().Contain(c => c.DisplayOrder == 2);
        result.Categories.Should().Contain(c => c.DisplayOrder == 3);
    }

    [Fact]
    public async Task GetFullMenuAsync_WithEmptyMenu_ShouldReturnEmptyCategories()
    {
        // Arrange
        var restaurant = await SeedRestaurantAsync();

        // Act
        var result = await _sut.GetFullMenuAsync(restaurant.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Categories.Should().BeEmpty();
    }

    // ═════════════════════════════════════════════
    //  CreateCategoryAsync Tests
    // ═════════════════════════════════════════════

    [Fact]
    public async Task CreateCategoryAsync_WithExistingRestaurant_ShouldCreateCategory()
    {
        // Arrange
        var restaurant = await SeedRestaurantAsync();
        var dto = new CreateCategoryDto { Name = "Başlangıçlar", DisplayOrder = 1 };

        // Act
        var result = await _sut.CreateCategoryAsync(restaurant.Id, dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Başlangıçlar");
        result.DisplayOrder.Should().Be(1);
        result.RestaurantId.Should().Be(restaurant.Id);
        result.Products.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateCategoryAsync_WithNonExistingRestaurant_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var dto = new CreateCategoryDto { Name = "Test" };

        // Act
        var act = () => _sut.CreateCategoryAsync(Guid.NewGuid(), dto);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task CreateCategoryAsync_ShouldSaveToDatabase()
    {
        // Arrange
        var restaurant = await SeedRestaurantAsync();
        var dto = new CreateCategoryDto { Name = "DB Kategori", DisplayOrder = 0 };

        // Act
        var result = await _sut.CreateCategoryAsync(restaurant.Id, dto);

        // Assert
        var dbCategory = await _db.MenuCategories.FindAsync(result.Id);
        dbCategory.Should().NotBeNull();
        dbCategory!.Name.Should().Be("DB Kategori");
    }

    [Fact]
    public async Task CreateCategoryAsync_ShouldPublishMenuUpdatedEvent()
    {
        // Arrange
        var restaurant = await SeedRestaurantAsync();
        var dto = new CreateCategoryDto { Name = "Event Test" };

        // Act
        await _sut.CreateCategoryAsync(restaurant.Id, dto);

        // Assert
        _publishEndpointMock.Verify(
            x => x.Publish(
                It.Is<MenuUpdatedEvent>(e =>
                    e.RestaurantId == restaurant.Id &&
                    e.Action == "Created" &&
                    e.EntityType == "Category"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═════════════════════════════════════════════
    //  UpdateCategoryAsync Tests
    // ═════════════════════════════════════════════

    [Fact]
    public async Task UpdateCategoryAsync_WithExistingCategory_ShouldUpdateAndReturn()
    {
        // Arrange
        var restaurant = await SeedRestaurantAsync();
        var category = await SeedCategoryAsync(restaurant.Id, "Eski Kategori", 1);
        var dto = new UpdateCategoryDto { Name = "Yeni Kategori", DisplayOrder = 5 };

        // Act
        var result = await _sut.UpdateCategoryAsync(category.Id, dto);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Yeni Kategori");
        result.DisplayOrder.Should().Be(5);
    }

    [Fact]
    public async Task UpdateCategoryAsync_WithNonExistingCategory_ShouldReturnNull()
    {
        // Arrange
        var dto = new UpdateCategoryDto { Name = "Test" };

        // Act
        var result = await _sut.UpdateCategoryAsync(Guid.NewGuid(), dto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateCategoryAsync_ShouldPublishMenuUpdatedEvent()
    {
        // Arrange
        var restaurant = await SeedRestaurantAsync();
        var category = await SeedCategoryAsync(restaurant.Id);
        var dto = new UpdateCategoryDto { Name = "Updated", DisplayOrder = 2 };

        // Act
        await _sut.UpdateCategoryAsync(category.Id, dto);

        // Assert
        _publishEndpointMock.Verify(
            x => x.Publish(
                It.Is<MenuUpdatedEvent>(e =>
                    e.RestaurantId == restaurant.Id &&
                    e.Action == "Updated" &&
                    e.EntityType == "Category" &&
                    e.EntityId == category.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═════════════════════════════════════════════
    //  DeleteCategoryAsync Tests
    // ═════════════════════════════════════════════

    [Fact]
    public async Task DeleteCategoryAsync_WithExistingCategory_ShouldDeleteAndReturnTrue()
    {
        // Arrange
        var restaurant = await SeedRestaurantAsync();
        var category = await SeedCategoryAsync(restaurant.Id);

        // Act
        var result = await _sut.DeleteCategoryAsync(category.Id);

        // Assert
        result.Should().BeTrue();
        var dbCategory = await _db.MenuCategories.FindAsync(category.Id);
        dbCategory.Should().BeNull();
    }

    [Fact]
    public async Task DeleteCategoryAsync_WithNonExistingCategory_ShouldReturnFalse()
    {
        // Act
        var result = await _sut.DeleteCategoryAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteCategoryAsync_ShouldPublishMenuUpdatedEvent()
    {
        // Arrange
        var restaurant = await SeedRestaurantAsync();
        var category = await SeedCategoryAsync(restaurant.Id);
        var categoryId = category.Id;

        // Act
        await _sut.DeleteCategoryAsync(categoryId);

        // Assert
        _publishEndpointMock.Verify(
            x => x.Publish(
                It.Is<MenuUpdatedEvent>(e =>
                    e.RestaurantId == restaurant.Id &&
                    e.Action == "Deleted" &&
                    e.EntityType == "Category"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═════════════════════════════════════════════
    //  CreateProductAsync Tests
    // ═════════════════════════════════════════════

    [Fact]
    public async Task CreateProductAsync_WithExistingCategory_ShouldCreateProduct()
    {
        // Arrange
        var restaurant = await SeedRestaurantAsync();
        var category = await SeedCategoryAsync(restaurant.Id);
        var dto = new CreateProductDto
        {
            Name = "Adana Kebap",
            Description = "Acılı kebap",
            Price = 120m,
            ImageUrl = "https://img.com/adana.jpg"
        };

        // Act
        var result = await _sut.CreateProductAsync(category.Id, dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Adana Kebap");
        result.Price.Should().Be(120m);
        result.IsAvailable.Should().BeTrue();
        result.CategoryId.Should().Be(category.Id);
    }

    [Fact]
    public async Task CreateProductAsync_WithNonExistingCategory_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var dto = new CreateProductDto { Name = "Test", Price = 10m };

        // Act
        var act = () => _sut.CreateProductAsync(Guid.NewGuid(), dto);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task CreateProductAsync_ShouldPublishMenuUpdatedEvent()
    {
        // Arrange
        var restaurant = await SeedRestaurantAsync();
        var category = await SeedCategoryAsync(restaurant.Id);
        var dto = new CreateProductDto { Name = "Event Product", Price = 50m };

        // Act
        await _sut.CreateProductAsync(category.Id, dto);

        // Assert
        _publishEndpointMock.Verify(
            x => x.Publish(
                It.Is<MenuUpdatedEvent>(e =>
                    e.RestaurantId == restaurant.Id &&
                    e.Action == "Created" &&
                    e.EntityType == "Product"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═════════════════════════════════════════════
    //  UpdateProductAsync Tests
    // ═════════════════════════════════════════════

    [Fact]
    public async Task UpdateProductAsync_WithExistingProduct_ShouldUpdateAndReturn()
    {
        // Arrange
        var restaurant = await SeedRestaurantAsync();
        var category = await SeedCategoryAsync(restaurant.Id);
        var product = await SeedProductAsync(category.Id, "Eski Ürün", 50m);

        var dto = new UpdateProductDto
        {
            Name = "Yeni Ürün",
            Description = "Yeni açıklama",
            Price = 75m,
            ImageUrl = "https://new-image.jpg"
        };

        // Act
        var result = await _sut.UpdateProductAsync(product.Id, dto);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Yeni Ürün");
        result.Price.Should().Be(75m);
    }

    [Fact]
    public async Task UpdateProductAsync_WithNonExistingProduct_ShouldReturnNull()
    {
        // Arrange
        var dto = new UpdateProductDto { Name = "Test", Price = 10m };

        // Act
        var result = await _sut.UpdateProductAsync(Guid.NewGuid(), dto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateProductAsync_WithPriceChange_ShouldPublishPriceChangedEvent()
    {
        // Arrange
        var restaurant = await SeedRestaurantAsync();
        var category = await SeedCategoryAsync(restaurant.Id);
        var product = await SeedProductAsync(category.Id, "Fiyat Test", 50m);

        var dto = new UpdateProductDto
        {
            Name = "Fiyat Test",
            Price = 75m // Fiyat değişti
        };

        // Act
        await _sut.UpdateProductAsync(product.Id, dto);

        // Assert
        _publishEndpointMock.Verify(
            x => x.Publish(
                It.Is<PriceChangedEvent>(e =>
                    e.ProductId == product.Id &&
                    e.OldPrice == 50m &&
                    e.NewPrice == 75m),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateProductAsync_WithSamePrice_ShouldNotPublishPriceChangedEvent()
    {
        // Arrange
        var restaurant = await SeedRestaurantAsync();
        var category = await SeedCategoryAsync(restaurant.Id);
        var product = await SeedProductAsync(category.Id, "Same Price", 50m);

        var dto = new UpdateProductDto
        {
            Name = "Same Price Updated",
            Price = 50m // Aynı fiyat
        };

        // Act
        await _sut.UpdateProductAsync(product.Id, dto);

        // Assert
        _publishEndpointMock.Verify(
            x => x.Publish(
                It.IsAny<PriceChangedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ═════════════════════════════════════════════
    //  ToggleProductStockAsync Tests
    // ═════════════════════════════════════════════

    [Fact]
    public async Task ToggleProductStockAsync_ShouldToggleAvailability()
    {
        // Arrange
        var restaurant = await SeedRestaurantAsync();
        var category = await SeedCategoryAsync(restaurant.Id);
        var product = await SeedProductAsync(category.Id); // IsAvailable = true

        var dto = new UpdateStockDto { IsAvailable = false };

        // Act
        var result = await _sut.ToggleProductStockAsync(product.Id, dto);

        // Assert
        result.Should().NotBeNull();
        result!.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleProductStockAsync_WithNonExistingProduct_ShouldReturnNull()
    {
        // Arrange
        var dto = new UpdateStockDto { IsAvailable = false };

        // Act
        var result = await _sut.ToggleProductStockAsync(Guid.NewGuid(), dto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ToggleProductStockAsync_ShouldPublishProductStockChangedEvent()
    {
        // Arrange
        var restaurant = await SeedRestaurantAsync();
        var category = await SeedCategoryAsync(restaurant.Id);
        var product = await SeedProductAsync(category.Id);

        var dto = new UpdateStockDto { IsAvailable = false };

        // Act
        await _sut.ToggleProductStockAsync(product.Id, dto);

        // Assert
        _publishEndpointMock.Verify(
            x => x.Publish(
                It.Is<ProductStockChangedEvent>(e =>
                    e.ProductId == product.Id &&
                    e.RestaurantId == restaurant.Id &&
                    e.IsAvailable == false),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═════════════════════════════════════════════
    //  DeleteProductAsync Tests
    // ═════════════════════════════════════════════

    [Fact]
    public async Task DeleteProductAsync_WithExistingProduct_ShouldDeleteAndReturnTrue()
    {
        // Arrange
        var restaurant = await SeedRestaurantAsync();
        var category = await SeedCategoryAsync(restaurant.Id);
        var product = await SeedProductAsync(category.Id);

        // Act
        var result = await _sut.DeleteProductAsync(product.Id);

        // Assert
        result.Should().BeTrue();
        var dbProduct = await _db.Products.FindAsync(product.Id);
        dbProduct.Should().BeNull();
    }

    [Fact]
    public async Task DeleteProductAsync_WithNonExistingProduct_ShouldReturnFalse()
    {
        // Act
        var result = await _sut.DeleteProductAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteProductAsync_ShouldPublishMenuUpdatedEvent()
    {
        // Arrange
        var restaurant = await SeedRestaurantAsync();
        var category = await SeedCategoryAsync(restaurant.Id);
        var product = await SeedProductAsync(category.Id);

        // Act
        await _sut.DeleteProductAsync(product.Id);

        // Assert
        _publishEndpointMock.Verify(
            x => x.Publish(
                It.Is<MenuUpdatedEvent>(e =>
                    e.RestaurantId == restaurant.Id &&
                    e.Action == "Deleted" &&
                    e.EntityType == "Product"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═════════════════════════════════════════════
    //  Product Mapping Tests
    // ═════════════════════════════════════════════

    [Fact]
    public async Task CreateProductAsync_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var restaurant = await SeedRestaurantAsync();
        var category = await SeedCategoryAsync(restaurant.Id);
        var dto = new CreateProductDto
        {
            Name = "Mapping Test Ürün",
            Description = "Test açıklama",
            Price = 99.99m,
            ImageUrl = "https://img.com/test.jpg"
        };

        // Act
        var result = await _sut.CreateProductAsync(category.Id, dto);

        // Assert
        result.Name.Should().Be("Mapping Test Ürün");
        result.Description.Should().Be("Test açıklama");
        result.Price.Should().Be(99.99m);
        result.ImageUrl.Should().Be("https://img.com/test.jpg");
        result.IsAvailable.Should().BeTrue();
        result.CategoryId.Should().Be(category.Id);
        result.Id.Should().NotBeEmpty();
    }
}
