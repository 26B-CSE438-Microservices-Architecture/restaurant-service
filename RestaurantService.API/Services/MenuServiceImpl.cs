using Microsoft.EntityFrameworkCore;
using RestaurantService.API.Data;
using RestaurantService.API.DTOs;
using RestaurantService.API.Entities;
using MassTransit;
using RestaurantService.API.IntegrationEvents;

namespace RestaurantService.API.Services;

public class MenuServiceImpl : IMenuService
{
    private readonly AppDbContext _db;
    private readonly IPublishEndpoint _publishEndpoint;

    public MenuServiceImpl(AppDbContext db, IPublishEndpoint publishEndpoint)
    {
        _db = db;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<MenuDto?> GetFullMenuAsync(Guid restaurantId)
    {
        var restaurant = await _db.Restaurants
            .Include(r => r.MenuCategories.OrderBy(mc => mc.DisplayOrder))
                .ThenInclude(mc => mc.Products)
            .FirstOrDefaultAsync(r => r.Id == restaurantId);

        if (restaurant == null) return null;

        return new MenuDto
        {
            RestaurantId = restaurant.Id,
            RestaurantName = restaurant.Name,
            Categories = restaurant.MenuCategories.Select(mc => new CategoryDto
            {
                Id = mc.Id,
                RestaurantId = mc.RestaurantId,
                Name = mc.Name,
                DisplayOrder = mc.DisplayOrder,
                Products = mc.Products.Select(p => MapToProductDto(p)).ToList()
            }).ToList()
        };
    }

    // ── Category Operations ──

    public async Task<CategoryDto> CreateCategoryAsync(Guid restaurantId, CreateCategoryDto dto)
    {
        var restaurant = await _db.Restaurants.FindAsync(restaurantId);
        if (restaurant == null)
            throw new KeyNotFoundException($"Restaurant {restaurantId} not found.");

        var category = new MenuCategory
        {
            RestaurantId = restaurantId,
            Name = dto.Name,
            DisplayOrder = dto.DisplayOrder
        };

        _db.MenuCategories.Add(category);
        await _db.SaveChangesAsync();

        await _publishEndpoint.Publish(new MenuUpdatedEvent(
            restaurantId,
            "Created",
            "Category",
            category.Id,
            DateTime.UtcNow
        ));

        return new CategoryDto
        {
            Id = category.Id,
            RestaurantId = category.RestaurantId,
            Name = category.Name,
            DisplayOrder = category.DisplayOrder,
            Products = new()
        };
    }

    public async Task<CategoryDto?> UpdateCategoryAsync(Guid categoryId, UpdateCategoryDto dto)
    {
        var category = await _db.MenuCategories
            .Include(mc => mc.Products)
            .FirstOrDefaultAsync(mc => mc.Id == categoryId);

        if (category == null) return null;

        category.Name = dto.Name;
        category.DisplayOrder = dto.DisplayOrder;

        await _db.SaveChangesAsync();

        await _publishEndpoint.Publish(new MenuUpdatedEvent(
            category.RestaurantId,
            "Updated",
            "Category",
            category.Id,
            DateTime.UtcNow
        ));

        return new CategoryDto
        {
            Id = category.Id,
            RestaurantId = category.RestaurantId,
            Name = category.Name,
            DisplayOrder = category.DisplayOrder,
            Products = category.Products.Select(p => MapToProductDto(p)).ToList()
        };
    }

    public async Task<bool> DeleteCategoryAsync(Guid categoryId)
    {
        var category = await _db.MenuCategories.FindAsync(categoryId);
        if (category == null) return false;

        var restaurantId = category.RestaurantId;

        _db.MenuCategories.Remove(category);
        await _db.SaveChangesAsync();

        await _publishEndpoint.Publish(new MenuUpdatedEvent(
            restaurantId,
            "Deleted",
            "Category",
            categoryId,
            DateTime.UtcNow
        ));
        return true;
    }

    // ── Product Operations ──

    public async Task<ProductDto> CreateProductAsync(Guid categoryId, CreateProductDto dto)
    {
        var category = await _db.MenuCategories.FindAsync(categoryId);
        if (category == null)
            throw new KeyNotFoundException($"Category {categoryId} not found.");

        var product = new Product
        {
            CategoryId = categoryId,
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            ImageUrl = dto.ImageUrl,
            IsAvailable = true
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        await _publishEndpoint.Publish(new MenuUpdatedEvent(
            category.RestaurantId,
            "Created",
            "Product",
            product.Id,
            DateTime.UtcNow
        ));

        return MapToProductDto(product);
    }

    public async Task<ProductDto?> UpdateProductAsync(Guid productId, UpdateProductDto dto)
    {
        var product = await _db.Products.FindAsync(productId);
        if (product == null) return null;

        var oldPrice = product.Price;
        
        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.ImageUrl = dto.ImageUrl;

        await _db.SaveChangesAsync();

        var category = await _db.MenuCategories.FindAsync(product.CategoryId);
        if (category != null)
        {
            if (oldPrice != dto.Price)
            {
                await _publishEndpoint.Publish(new PriceChangedEvent(product.Id, category.RestaurantId, oldPrice, dto.Price, DateTime.UtcNow));
            }
            await _publishEndpoint.Publish(new MenuUpdatedEvent(category.RestaurantId, "Updated", "Product", product.Id, DateTime.UtcNow));
        }
        return MapToProductDto(product);
    }

    public async Task<ProductDto?> ToggleProductStockAsync(Guid productId, UpdateStockDto dto)
    {
        var product = await _db.Products.FindAsync(productId);
        if (product == null) return null;

        product.IsAvailable = dto.IsAvailable;

        await _db.SaveChangesAsync();

        var category = await _db.MenuCategories.FindAsync(product.CategoryId);
        if (category != null)
        {
            await _publishEndpoint.Publish(new ProductStockChangedEvent(product.Id, category.RestaurantId, product.IsAvailable, DateTime.UtcNow));
        }
        return MapToProductDto(product);
    }

    public async Task<bool> DeleteProductAsync(Guid productId)
    {
        var product = await _db.Products.FindAsync(productId);
        if (product == null) return false;

        var categoryId = product.CategoryId;

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();

        var category = await _db.MenuCategories.FindAsync(categoryId);
        if (category != null)
        {
            await _publishEndpoint.Publish(new MenuUpdatedEvent(category.RestaurantId, "Deleted", "Product", productId, DateTime.UtcNow));
        }
        return true;
    }

    // ── Helpers ──

    private static ProductDto MapToProductDto(Product p) => new()
    {
        Id = p.Id,
        CategoryId = p.CategoryId,
        Name = p.Name,
        Description = p.Description,
        Price = p.Price,
        IsAvailable = p.IsAvailable,
        ImageUrl = p.ImageUrl
    };
}
