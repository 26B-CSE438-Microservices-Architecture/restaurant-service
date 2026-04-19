using Microsoft.AspNetCore.Mvc;
using RestaurantService.API.DTOs;
using RestaurantService.API.Services;

namespace RestaurantService.API.Controllers;

[ApiController]
[Route("api/v1/restaurants")]
public class RestaurantsController : ControllerBase
{
    private readonly IRestaurantService _restaurantService;

    public RestaurantsController(IRestaurantService restaurantService)
    {
        _restaurantService = restaurantService;
    }

    /// <summary>
    /// List/Search restaurants with optional filters
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<RestaurantSummaryDto>>> GetAll(
        [FromQuery] string? name,
        [FromQuery] string? cuisine,
        [FromQuery] double? lat,
        [FromQuery] double? lng)
    {
        var restaurants = await _restaurantService.GetAllAsync(name, cuisine, lat, lng);
        return Ok(restaurants);
    }

    /// <summary>
    /// Get restaurant by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<RestaurantDto>> GetById(Guid id)
    {
        var restaurant = await _restaurantService.GetByIdAsync(id);
        if (restaurant == null) return NotFound();
        return Ok(restaurant);
    }

    /// <summary>
    /// Get minimal restaurant info for the user-service "Favorite Restaurants" screen.
    /// Returns only: name, type, opening hours, location and open/closed status.
    /// </summary>
    [HttpGet("{id}/favorite-info")]
    public async Task<ActionResult<FavoriteRestaurantInfoDto>> GetFavoriteInfo(Guid id)
    {
        var info = await _restaurantService.GetFavoriteInfoAsync(id);
        if (info == null) return NotFound();
        return Ok(info);
    }

    /// <summary>
    /// Get nearby restaurants within radius
    /// </summary>
    [HttpGet("nearby")]
    public async Task<ActionResult<List<RestaurantSummaryDto>>> GetNearby(
        [FromQuery] double lat,
        [FromQuery] double lng,
        [FromQuery] double radiusKm = 10)
    {
        var restaurants = await _restaurantService.GetNearbyAsync(lat, lng, radiusKm);
        return Ok(restaurants);
    }

    /// <summary>
    /// Register a new restaurant
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<RestaurantDto>> Create([FromBody] CreateRestaurantDto dto)
    {
        var restaurant = await _restaurantService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = restaurant.Id }, restaurant);
    }

    /// <summary>
    /// Update restaurant profile
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<RestaurantDto>> Update(Guid id, [FromBody] UpdateRestaurantDto dto)
    {
        var restaurant = await _restaurantService.UpdateAsync(id, dto);
        if (restaurant == null) return NotFound();
        return Ok(restaurant);
    }

    /// <summary>
    /// Toggle restaurant status (Open/Closed/Busy) — Emergency toggle
    /// </summary>
    [HttpPatch("{id}/status")]
    public async Task<ActionResult<RestaurantDto>> UpdateStatus(Guid id, [FromBody] UpdateStatusDto dto)
    {
        try
        {
            var restaurant = await _restaurantService.UpdateStatusAsync(id, dto);
            if (restaurant == null) return NotFound();
            return Ok(restaurant);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a restaurant
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _restaurantService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
