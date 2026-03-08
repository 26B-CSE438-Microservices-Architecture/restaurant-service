using System.ComponentModel.DataAnnotations;

namespace RestaurantService.API.Entities;

public class Product
{
    public Guid Id { get; set; }

    public Guid CategoryId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public bool IsAvailable { get; set; } = true;

    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    // Navigation
    public MenuCategory Category { get; set; } = null!;
}
