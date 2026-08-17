using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Seatsure.Domain;
namespace Seatsure.Application.Interfaces; 
public  interface IEventRepository
 {
    Task<Event?> GetByIdAsync(Guid id);
    Task<(IEnumerable<Event> events, int Totalcount)> GetPublishedAsync(int page, int pageSize);

    Task AddAsync(Event _event);

}

