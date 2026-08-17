using Seatsure.BLL.DTOs.TicketTypes;

namespace Seatsure.BLL.Services.Interfaces;

public interface ITicketTypeService
{
    Task<IEnumerable<TicketTypeDto>> GetByEventIdAsync(Guid eventId);
    Task<TicketTypeDto> AddAsync(Guid eventId, Guid organizerId, CreateTicketTypeRequest request);
}
