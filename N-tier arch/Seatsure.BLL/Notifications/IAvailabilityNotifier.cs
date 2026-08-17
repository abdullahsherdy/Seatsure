namespace Seatsure.BLL.Notifications;

/// <summary>
/// Port for broadcasting availability changes (README §3.6). Defined in the BLL so services
/// depend on an abstraction; the SignalR-backed implementation is wired in the API layer.
/// Until then, a no-op implementation is registered.
/// </summary>
public interface IAvailabilityNotifier
{
    Task AvailabilityChangedAsync(Guid ticketTypeId, int availableQuantity);
}
