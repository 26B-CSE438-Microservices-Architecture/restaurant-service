using System.ComponentModel.DataAnnotations;

namespace RestaurantService.API.Entities;

public class Restaurant
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(100)]
    public string CuisineType { get; set; } = string.Empty;

    [MaxLength(500)]
    public string AddressText { get; set; } = string.Empty;

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    [MaxLength(500)]
    public string LogoUrl { get; set; } = string.Empty;

    public decimal MinOrderAmount { get; set; }
    public decimal DeliveryFee { get; set; }

    public bool IsActive { get; set; } = true;
    public RestaurantStatus Status { get; set; } = RestaurantStatus.Open;

    public TimeOnly OpeningTime { get; set; }
    public TimeOnly ClosingTime { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<MenuCategory> MenuCategories { get; set; } = new List<MenuCategory>();
}
