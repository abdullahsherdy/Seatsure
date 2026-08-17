namespace Seatsure.BLL.Notifications;

/// <summary>No-op notifier used until the SignalR hub is added in a later phase.</summary>
internal sealed class NullAvailabilityNotifier : IAvailabilityNotifier
{
    public Task AvailabilityChangedAsync(Guid ticketTypeId, int availableQuantity) => Task.CompletedTask;
}
