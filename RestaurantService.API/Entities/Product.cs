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

    /// <summary>
    /// Mevcut stok miktarı. Sipariş onaylanırken rezerve edilir, iptal/red durumunda
    /// geri yüklenir (compensation). Mevcut REST akışı bu alanı kullanmadığı için
    /// default 100 ile gelir; sadece async sipariş onay/iptal akışında gerekli.
    /// </summary>
    public int StockQuantity { get; set; } = 100;

    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    // Navigation
    public MenuCategory Category { get; set; } = null!;
}
