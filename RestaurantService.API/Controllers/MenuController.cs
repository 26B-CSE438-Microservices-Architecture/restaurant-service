using Microsoft.AspNetCore.Mvc;
using RestaurantService.API.DTOs;
using RestaurantService.API.Services;

namespace RestaurantService.API.Controllers;

[ApiController]
[Route("api/v1")]
public class MenuController : ControllerBase
{
    private readonly IMenuService _menuService;

    public MenuController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    /// <summary>
    /// Get full menu for a restaurant (categories + products)
    /// </summary>
    [HttpGet("restaurants/{restaurantId}/menu")]
    public async Task<ActionResult<MenuDto>> GetFullMenu(Guid restaurantId)
    {
        var menu = await _menuService.GetFullMenuAsync(restaurantId);
        if (menu == null) return NotFound();
        return Ok(menu);
    }

    // ── Category Endpoints ──

    /// <summary>
    /// Add a new category to a restaurant's menu
    /// </summary>
    [HttpPost("restaurants/{restaurantId}/categories")]
    public async Task<ActionResult<CategoryDto>> CreateCategory(Guid restaurantId, [FromBody] CreateCategoryDto dto)
    {
        try
        {
            var category = await _menuService.CreateCategoryAsync(restaurantId, dto);
            return Created($"/api/v1/categories/{category.Id}", category);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update a menu category
    /// </summary>
    [HttpPut("categories/{id}")]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(Guid id, [FromBody] UpdateCategoryDto dto)
    {
        var category = await _menuService.UpdateCategoryAsync(id, dto);
        if (category == null) return NotFound();
        return Ok(category);
    }

    /// <summary>
    /// Delete a menu category
    /// </summary>
    [HttpDelete("categories/{id}")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var deleted = await _menuService.DeleteCategoryAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    // ── Product Endpoints ──

    /// <summary>
    /// Add a new product to a category
    /// </summary>
    [HttpPost("categories/{categoryId}/products")]
    public async Task<ActionResult<ProductDto>> CreateProduct(Guid categoryId, [FromBody] CreateProductDto dto)
    {
        try
        {
            var product = await _menuService.CreateProductAsync(categoryId, dto);
            return Created($"/api/v1/products/{product.Id}", product);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update a product (price, description, etc.)
    /// </summary>
    [HttpPut("products/{id}")]
    public async Task<ActionResult<ProductDto>> UpdateProduct(Guid id, [FromBody] UpdateProductDto dto)
    {
        var product = await _menuService.UpdateProductAsync(id, dto);
        if (product == null) return NotFound();
        return Ok(product);
    }

    /// <summary>
    /// Toggle product stock (available/out of stock)
    /// </summary>
    [HttpPatch("products/{id}/stock")]
    public async Task<ActionResult<ProductDto>> ToggleProductStock(Guid id, [FromBody] UpdateStockDto dto)
    {
        var product = await _menuService.ToggleProductStockAsync(id, dto);
        if (product == null) return NotFound();
        return Ok(product);
    }

    /// <summary>
    /// Delete a product
    /// </summary>
    [HttpDelete("products/{id}")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var deleted = await _menuService.DeleteProductAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
