using RestaurantService.API.DTOs;

namespace RestaurantService.API.Services;

public interface IMenuService
{
    Task<MenuDto?> GetFullMenuAsync(Guid restaurantId);

    // Categories
    Task<CategoryDto> CreateCategoryAsync(Guid restaurantId, CreateCategoryDto dto);
    Task<CategoryDto?> UpdateCategoryAsync(Guid categoryId, UpdateCategoryDto dto);
    Task<bool> DeleteCategoryAsync(Guid categoryId);

    // Products
    Task<ProductDto> CreateProductAsync(Guid categoryId, CreateProductDto dto);
    Task<ProductDto?> UpdateProductAsync(Guid productId, UpdateProductDto dto);
    Task<ProductDto?> ToggleProductStockAsync(Guid productId, UpdateStockDto dto);
    Task<bool> DeleteProductAsync(Guid productId);
}
