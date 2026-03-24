using Microsoft.EntityFrameworkCore;
using RestaurantService.API.Data;
using RestaurantService.API.DTOs;
using RestaurantService.API.Entities;
using MassTransit;
using RestaurantService.API.IntegrationEvents;

namespace RestaurantService.API.Services;

public class RestaurantServiceImpl : IRestaurantService
{
    private readonly AppDbContext _db;
    private readonly IPublishEndpoint _publishEndpoint;

    public RestaurantServiceImpl(AppDbContext db, IPublishEndpoint publishEndpoint)
    {
        _db = db;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<List<RestaurantSummaryDto>> GetAllAsync(string? name, string? cuisine, double? lat, double? lng)
    {
        var query = _db.Restaurants
            .Where(r => r.IsActive)
            .AsQueryable();

        // Filter by name
        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(r => r.Name.ToLower().Contains(name.ToLower()));
        }

        // Filter by cuisine/description
        if (!string.IsNullOrWhiteSpace(cuisine))
        {
            query = query.Where(r => r.Description.ToLower().Contains(cuisine.ToLower()));
        }

        var restaurants = await query.ToListAsync();

        var result = restaurants.Select(r =>
        {
            var dto = MapToSummaryDto(r);

            // Calculate distance if user coords provided
            if (lat.HasValue && lng.HasValue)
            {
                dto.DistanceKm = CalculateDistanceKm(lat.Value, lng.Value, r.Latitude, r.Longitude);
            }

            return dto;
        }).ToList();

        // Sort by distance if coordinates provided
        if (lat.HasValue && lng.HasValue)
        {
            result = result.OrderBy(r => r.DistanceKm).ToList();
        }

        return result;
    }

    public async Task<RestaurantDto?> GetByIdAsync(Guid id)
    {
        var restaurant = await _db.Restaurants.FindAsync(id);
        return restaurant == null ? null : MapToDto(restaurant);
    }

    public async Task<List<RestaurantSummaryDto>> GetNearbyAsync(double lat, double lng, double radiusKm)
    {
        var restaurants = await _db.Restaurants
            .Where(r => r.IsActive)
            .ToListAsync();

        return restaurants
            .Select(r =>
            {
                var dto = MapToSummaryDto(r);
                dto.DistanceKm = CalculateDistanceKm(lat, lng, r.Latitude, r.Longitude);
                return dto;
            })
            .Where(r => r.DistanceKm <= radiusKm)
            .OrderBy(r => r.DistanceKm)
            .ToList();
    }

    public async Task<RestaurantDto> CreateAsync(CreateRestaurantDto dto)
    {
        var restaurant = new Restaurant
        {
            Name = dto.Name,
            Description = dto.Description,
            AddressText = dto.AddressText,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            LogoUrl = dto.LogoUrl,
            MinOrderAmount = dto.MinOrderAmount,
            DeliveryFee = dto.DeliveryFee,
            OpeningTime = dto.OpeningTime,
            ClosingTime = dto.ClosingTime,
            IsActive = true,
            Status = RestaurantStatus.Open,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Restaurants.Add(restaurant);
        await _db.SaveChangesAsync();

        await _publishEndpoint.Publish(new RestaurantCreatedEvent(
            restaurant.Id,
            restaurant.Name,
            restaurant.Latitude,
            restaurant.Longitude,
            DateTime.UtcNow
        ));

        return MapToDto(restaurant);
    }

    public async Task<RestaurantDto?> UpdateAsync(Guid id, UpdateRestaurantDto dto)
    {
        var restaurant = await _db.Restaurants.FindAsync(id);
        if (restaurant == null) return null;

        restaurant.Name = dto.Name;
        restaurant.Description = dto.Description;
        restaurant.AddressText = dto.AddressText;
        restaurant.Latitude = dto.Latitude;
        restaurant.Longitude = dto.Longitude;
        restaurant.LogoUrl = dto.LogoUrl;
        restaurant.MinOrderAmount = dto.MinOrderAmount;
        restaurant.DeliveryFee = dto.DeliveryFee;
        restaurant.OpeningTime = dto.OpeningTime;
        restaurant.ClosingTime = dto.ClosingTime;
        restaurant.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        
        await _publishEndpoint.Publish(new RestaurantUpdatedEvent(
            restaurant.Id,
            new[] { "All" }, 
            DateTime.UtcNow
        ));
        return MapToDto(restaurant);
    }

    public async Task<RestaurantDto?> UpdateStatusAsync(Guid id, UpdateStatusDto dto)
    {
        var restaurant = await _db.Restaurants.FindAsync(id);
        if (restaurant == null) return null;

        if (!Enum.TryParse<RestaurantStatus>(dto.Status, true, out var status))
            throw new ArgumentException($"Invalid status: {dto.Status}. Use Open, Closed, or Busy.");

        var oldStatus = restaurant.Status.ToString();
        restaurant.Status = status;
        restaurant.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _publishEndpoint.Publish(new RestaurantStatusChangedEvent(
            restaurant.Id,
            oldStatus,
            restaurant.Status.ToString(),
            DateTime.UtcNow
        ));
        return MapToDto(restaurant);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var restaurant = await _db.Restaurants.FindAsync(id);
        if (restaurant == null) return false;

        _db.Restaurants.Remove(restaurant);
        await _db.SaveChangesAsync();

        await _publishEndpoint.Publish(new RestaurantDeletedEvent(
            id,
            DateTime.UtcNow
        ));
        return true;
    }

    // ── Helpers ──

    private static double CalculateDistanceKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371; // Earth radius in km

        var dLat = ToRad(lat2 - lat1);
        var dLng = ToRad(lng2 - lng1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return Math.Round(R * c, 2);
    }

    private static double ToRad(double deg) => deg * (Math.PI / 180);

    private static RestaurantDto MapToDto(Restaurant r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Description = r.Description,
        AddressText = r.AddressText,
        Latitude = r.Latitude,
        Longitude = r.Longitude,
        LogoUrl = r.LogoUrl,
        MinOrderAmount = r.MinOrderAmount,
        DeliveryFee = r.DeliveryFee,
        IsActive = r.IsActive,
        Status = r.Status.ToString(),
        OpeningTime = r.OpeningTime,
        ClosingTime = r.ClosingTime,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt
    };

    private static RestaurantSummaryDto MapToSummaryDto(Restaurant r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Description = r.Description,
        LogoUrl = r.LogoUrl,
        MinOrderAmount = r.MinOrderAmount,
        DeliveryFee = r.DeliveryFee,
        Status = r.Status.ToString()
    };
}
