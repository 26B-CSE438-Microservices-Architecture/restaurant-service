namespace RestaurantService.API.Entities;

/// <summary>
/// Stok rezervasyonunun saga (long-running transaction) içindeki durumu.
/// </summary>
public enum ReservationStatus
{
    /// <summary>Stok başarıyla rezerve edildi, sipariş onayı/ödemesi bekleniyor.</summary>
    Reserved = 0,

    /// <summary>Sipariş tamamen onaylandı (PAID), rezervasyon kalıcı olarak düşüldü.</summary>
    Confirmed = 1,

    /// <summary>Sipariş iptal/red edildi, stok geri açıldı (compensation).</summary>
    Released = 2
}
