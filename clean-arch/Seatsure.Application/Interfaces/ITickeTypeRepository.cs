using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Seatsure.Domain;
namespace Seatsure.Application.Interfaces;

public interface ITickeTypeRepository
{
    Task<TicketType?> GetTicketTypeByIdAsync(Guid id);

    Task<IEnumerable<TicketType>> GetByEventIdAsync(Guid eventId);

    Task AddAsync(TicketType ticketType);
}
