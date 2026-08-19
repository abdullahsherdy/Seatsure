using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seatsure.Application.Notifications
{
    public class NullAVailabilityNotifier : IAvailabilityNotification
    {
        // 
        public Task AvailabilityChangedAsync(Guid Ticketid, int availableQunatity) => Task.CompletedTask; 
    }
}
