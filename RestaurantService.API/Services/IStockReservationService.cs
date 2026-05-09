using RestaurantService.API.IntegrationEvents.Inbound;

namespace RestaurantService.API.Services;

/// <summary>
/// Sipariş için stok rezervasyonu / serbest bırakma işlemlerini yönetir.
/// Saga + Compensating Transaction pattern'inin merkezi.
/// </summary>
public interface IStockReservationService
{
    /// <summary>
    /// Sipariş için stok rezervasyonu yapmaya çalışır. Idempotent:
    /// aynı OrderId ile tekrar çağrılırsa duplicate rezervasyon oluşturmaz.
    /// </summary>
    Task<ReservationResult> TryReserveAsync(
        Guid orderId,
        Guid restaurantId,
        IReadOnlyList<OrderItemPayloadDto> items,
        CancellationToken ct = default);

    /// <summary>
    /// Sipariş için tüm rezervasyonları geri açar (compensation).
    /// Hiç rezervasyon yoksa no-op (idempotent).
    /// </summary>
    Task<ReleaseResult> ReleaseAsync(
        Guid orderId,
        string reason,
        CancellationToken ct = default);

    /// <summary>
    /// Sipariş onaylandığında rezervasyonları kalıcı olarak işaretler (Confirmed).
    /// </summary>
    Task ConfirmAsync(Guid orderId, CancellationToken ct = default);
}

public record ReservationResult(
    bool Success,
    string? FailureReason,
    IReadOnlyList<(Guid ProductId, int Quantity)> ReservedItems
);

public record ReleaseResult(
    IReadOnlyList<(Guid ProductId, int Quantity)> ReleasedItems
);
