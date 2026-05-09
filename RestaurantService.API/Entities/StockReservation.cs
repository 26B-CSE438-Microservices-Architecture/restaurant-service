namespace RestaurantService.API.Entities;

/// <summary>
/// Bir siparişin belirli bir ürün için yaptığı stok rezervasyon kaydı.
/// Saga pattern + Compensating Transaction için kullanılır:
///   - OrderApprovalRequested geldiğinde Reserved kaydı yaratılır + ürün stoğu düşülür
///   - OrderConfirmed geldiğinde Confirmed'e çekilir (kalıcı)
///   - OrderCancelled geldiğinde Released'a çekilir + ürün stoğu geri yüklenir
/// </summary>
public class StockReservation
{
    public Guid Id { get; set; }

    /// <summary>Sipariş servisinden gelen sipariş ID (correlation key).</summary>
    public Guid OrderId { get; set; }

    public Guid RestaurantId { get; set; }

    public Guid ProductId { get; set; }

    public int Quantity { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Reserved;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Product Product { get; set; } = null!;
}
