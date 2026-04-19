using RestaurantService.API.DTOs;

namespace RestaurantService.API.Services;

public interface IRestaurantService
{
    Task<List<RestaurantSummaryDto>> GetAllAsync(string? name, string? cuisine, double? lat, double? lng);
    Task<RestaurantDto?> GetByIdAsync(Guid id);
    Task<FavoriteRestaurantInfoDto?> GetFavoriteInfoAsync(Guid id);
    Task<List<RestaurantSummaryDto>> GetNearbyAsync(double lat, double lng, double radiusKm);
    Task<RestaurantDto> CreateAsync(CreateRestaurantDto dto);
    Task<RestaurantDto?> UpdateAsync(Guid id, UpdateRestaurantDto dto);
    Task<RestaurantDto?> UpdateStatusAsync(Guid id, UpdateStatusDto dto);
    Task<bool> DeleteAsync(Guid id);
}
