using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantService.API.Data;
using RestaurantService.API.DTOs;

namespace RestaurantService.API.Controllers;

/// <summary>
/// Internal endpoints — sadece diğer mikro servisler tarafından çağrılır.
/// Dışarıya açık değildir (Nginx / API Gateway bu rotayı bloklayabilir).
/// </summary>
[ApiController]
[Route("internal")]
public class InternalController : ControllerBase
{
    private readonly AppDbContext _db;

    public InternalController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Order Service'in sepete ürün eklerken fiyat ve stok doğrulaması için çağırdığı endpoint.
    /// </summary>
    /// <remarks>
    /// Order Service bu endpoint'i şu Feign client ile çağırıyor:
    ///   POST /internal/restaurants/{restaurantId}/validate-items
    ///   Body: [{ "menuItemId": "uuid", "quantity": 1 }]
    /// </remarks>
    [HttpPost("restaurants/{restaurantId}/validate-items")]
    public async Task<ActionResult<MenuValidationResponse>> ValidateItems(
        Guid restaurantId,
        [FromBody] List<ValidateItemsRequest> items)
    {
        if (items == null || items.Count == 0)
            return Ok(new MenuValidationResponse { Valid = false, ErrorMessage = "İstek boş." });

        // Restoranın tüm kategorilerini ve ürünlerini çek
        var restaurant = await _db.Restaurants
            .Include(r => r.MenuCategories)
                .ThenInclude(mc => mc.Products)
            .FirstOrDefaultAsync(r => r.Id == restaurantId);

        if (restaurant == null)
            return Ok(new MenuValidationResponse
            {
                Valid = false,
                ErrorMessage = $"Restoran bulunamadı: {restaurantId}"
            });

        // Tüm ürünleri düz bir sözlüğe al (hızlı lookup için)
        var allProducts = restaurant.MenuCategories
            .SelectMany(mc => mc.Products)
            .ToDictionary(p => p.Id);

        var validatedItems = new List<ValidatedItemResponse>();

        foreach (var item in items)
        {
            if (!allProducts.TryGetValue(item.MenuItemId, out var product))
            {
                // Ürün bu restoranda yok
                return Ok(new MenuValidationResponse
                {
                    Valid = false,
                    ErrorMessage = $"Ürün bulunamadı: {item.MenuItemId}"
                });
            }

            validatedItems.Add(new ValidatedItemResponse
            {
                MenuItemId = product.Id,
                Name = product.Name,
                Price = product.Price,
                Available = product.IsAvailable
            });
        }

        return Ok(new MenuValidationResponse
        {
            Valid = true,
            Items = validatedItems
        });
    }
}
