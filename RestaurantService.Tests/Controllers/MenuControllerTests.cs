using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RestaurantService.API.Controllers;
using RestaurantService.API.DTOs;
using RestaurantService.API.Services;

namespace RestaurantService.Tests.Controllers;

public class MenuControllerTests
{
    private readonly Mock<IMenuService> _serviceMock;
    private readonly MenuController _sut;

    public MenuControllerTests()
    {
        _serviceMock = new Mock<IMenuService>();
        _sut = new MenuController(_serviceMock.Object);
    }

    // ═════════════════════════════════════════════
    //  GetFullMenu Tests
    // ═════════════════════════════════════════════

    [Fact]
    public async Task GetFullMenu_WithExistingRestaurant_ShouldReturnOkWithMenu()
    {
        // Arrange
        var restaurantId = Guid.NewGuid();
        var menu = new MenuDto
        {
            RestaurantId = restaurantId,
            RestaurantName = "Test Restaurant",
            Categories = new List<CategoryDto>
            {
                new() { Id = Guid.NewGuid(), Name = "Ana Yemekler", Products = new List<ProductDto>() }
            }
        };
        _serviceMock.Setup(s => s.GetFullMenuAsync(restaurantId)).ReturnsAsync(menu);

        // Act
        var result = await _sut.GetFullMenu(restaurantId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeOfType<MenuDto>().Subject;
        data.RestaurantId.Should().Be(restaurantId);
        data.Categories.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetFullMenu_WithNonExistingRestaurant_ShouldReturnNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetFullMenuAsync(It.IsAny<Guid>()))
            .ReturnsAsync((MenuDto?)null);

        // Act
        var result = await _sut.GetFullMenu(Guid.NewGuid());

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // ═════════════════════════════════════════════
    //  CreateCategory Tests
    // ═════════════════════════════════════════════

    [Fact]
    public async Task CreateCategory_WithExistingRestaurant_ShouldReturnCreated()
    {
        // Arrange
        var restaurantId = Guid.NewGuid();
        var dto = new CreateCategoryDto { Name = "Tatlılar", DisplayOrder = 3 };
        var created = new CategoryDto
        {
            Id = Guid.NewGuid(),
            RestaurantId = restaurantId,
            Name = "Tatlılar",
            DisplayOrder = 3
        };
        _serviceMock.Setup(s => s.CreateCategoryAsync(restaurantId, dto)).ReturnsAsync(created);

        // Act
        var result = await _sut.CreateCategory(restaurantId, dto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedResult>().Subject;
        var data = createdResult.Value.Should().BeOfType<CategoryDto>().Subject;
        data.Name.Should().Be("Tatlılar");
    }

    [Fact]
    public async Task CreateCategory_WithNonExistingRestaurant_ShouldReturnNotFound()
    {
        // Arrange
        var dto = new CreateCategoryDto { Name = "Test" };
        _serviceMock.Setup(s => s.CreateCategoryAsync(It.IsAny<Guid>(), dto))
            .ThrowsAsync(new KeyNotFoundException("Restaurant not found."));

        // Act
        var result = await _sut.CreateCategory(Guid.NewGuid(), dto);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ═════════════════════════════════════════════
    //  UpdateCategory Tests
    // ═════════════════════════════════════════════

    [Fact]
    public async Task UpdateCategory_WithExistingCategory_ShouldReturnOk()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var dto = new UpdateCategoryDto { Name = "Updated", DisplayOrder = 5 };
        var updated = new CategoryDto { Id = categoryId, Name = "Updated", DisplayOrder = 5 };
        _serviceMock.Setup(s => s.UpdateCategoryAsync(categoryId, dto)).ReturnsAsync(updated);

        // Act
        var result = await _sut.UpdateCategory(categoryId, dto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeOfType<CategoryDto>().Subject;
        data.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task UpdateCategory_WithNonExistingCategory_ShouldReturnNotFound()
    {
        // Arrange
        var dto = new UpdateCategoryDto { Name = "Test" };
        _serviceMock.Setup(s => s.UpdateCategoryAsync(It.IsAny<Guid>(), dto))
            .ReturnsAsync((CategoryDto?)null);

        // Act
        var result = await _sut.UpdateCategory(Guid.NewGuid(), dto);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // ═════════════════════════════════════════════
    //  DeleteCategory Tests
    // ═════════════════════════════════════════════

    [Fact]
    public async Task DeleteCategory_WithExistingCategory_ShouldReturnNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.DeleteCategoryAsync(id)).ReturnsAsync(true);

        // Act
        var result = await _sut.DeleteCategory(id);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteCategory_WithNonExistingCategory_ShouldReturnNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.DeleteCategoryAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        // Act
        var result = await _sut.DeleteCategory(Guid.NewGuid());

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    // ═════════════════════════════════════════════
    //  CreateProduct Tests
    // ═════════════════════════════════════════════

    [Fact]
    public async Task CreateProduct_WithExistingCategory_ShouldReturnCreated()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var dto = new CreateProductDto { Name = "Kebap", Price = 120m };
        var created = new ProductDto
        {
            Id = Guid.NewGuid(),
            CategoryId = categoryId,
            Name = "Kebap",
            Price = 120m,
            IsAvailable = true
        };
        _serviceMock.Setup(s => s.CreateProductAsync(categoryId, dto)).ReturnsAsync(created);

        // Act
        var result = await _sut.CreateProduct(categoryId, dto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedResult>().Subject;
        var data = createdResult.Value.Should().BeOfType<ProductDto>().Subject;
        data.Name.Should().Be("Kebap");
    }

    [Fact]
    public async Task CreateProduct_WithNonExistingCategory_ShouldReturnNotFound()
    {
        // Arrange
        var dto = new CreateProductDto { Name = "Test", Price = 10m };
        _serviceMock.Setup(s => s.CreateProductAsync(It.IsAny<Guid>(), dto))
            .ThrowsAsync(new KeyNotFoundException("Category not found."));

        // Act
        var result = await _sut.CreateProduct(Guid.NewGuid(), dto);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ═════════════════════════════════════════════
    //  UpdateProduct Tests
    // ═════════════════════════════════════════════

    [Fact]
    public async Task UpdateProduct_WithExistingProduct_ShouldReturnOk()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var dto = new UpdateProductDto { Name = "Updated Product", Price = 150m };
        var updated = new ProductDto { Id = productId, Name = "Updated Product", Price = 150m };
        _serviceMock.Setup(s => s.UpdateProductAsync(productId, dto)).ReturnsAsync(updated);

        // Act
        var result = await _sut.UpdateProduct(productId, dto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeOfType<ProductDto>().Subject;
        data.Name.Should().Be("Updated Product");
    }

    [Fact]
    public async Task UpdateProduct_WithNonExistingProduct_ShouldReturnNotFound()
    {
        // Arrange
        var dto = new UpdateProductDto { Name = "Test", Price = 10m };
        _serviceMock.Setup(s => s.UpdateProductAsync(It.IsAny<Guid>(), dto))
            .ReturnsAsync((ProductDto?)null);

        // Act
        var result = await _sut.UpdateProduct(Guid.NewGuid(), dto);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // ═════════════════════════════════════════════
    //  ToggleProductStock Tests
    // ═════════════════════════════════════════════

    [Fact]
    public async Task ToggleProductStock_WithExistingProduct_ShouldReturnOk()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var dto = new UpdateStockDto { IsAvailable = false };
        var updated = new ProductDto { Id = productId, IsAvailable = false };
        _serviceMock.Setup(s => s.ToggleProductStockAsync(productId, dto)).ReturnsAsync(updated);

        // Act
        var result = await _sut.ToggleProductStock(productId, dto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeOfType<ProductDto>().Subject;
        data.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleProductStock_WithNonExistingProduct_ShouldReturnNotFound()
    {
        // Arrange
        var dto = new UpdateStockDto { IsAvailable = false };
        _serviceMock.Setup(s => s.ToggleProductStockAsync(It.IsAny<Guid>(), dto))
            .ReturnsAsync((ProductDto?)null);

        // Act
        var result = await _sut.ToggleProductStock(Guid.NewGuid(), dto);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // ═════════════════════════════════════════════
    //  DeleteProduct Tests
    // ═════════════════════════════════════════════

    [Fact]
    public async Task DeleteProduct_WithExistingProduct_ShouldReturnNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.DeleteProductAsync(id)).ReturnsAsync(true);

        // Act
        var result = await _sut.DeleteProduct(id);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteProduct_WithNonExistingProduct_ShouldReturnNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.DeleteProductAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        // Act
        var result = await _sut.DeleteProduct(Guid.NewGuid());

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }
}
