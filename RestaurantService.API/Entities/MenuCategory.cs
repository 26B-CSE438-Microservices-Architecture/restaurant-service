using System.ComponentModel.DataAnnotations;

namespace RestaurantService.API.Entities;

public class MenuCategory
{
    public Guid Id { get; set; }

    public Guid RestaurantId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    // Navigation
    public Restaurant Restaurant { get; set; } = null!;
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
