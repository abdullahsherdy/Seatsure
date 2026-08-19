using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// <summary>
/// Port for broadcasting availability changes
/// depend on an abstraction; the SignalR-backed implementation is wired in the API layer.
/// Until then, a no-op implementation is registered.
/// </summary>
namespace Seatsure.Application.Notifications;
public  interface IAvailabilityNotification
{
    Task AvailabilityChangedAsync(Guid Ticketid, int availableQunatity);
}
