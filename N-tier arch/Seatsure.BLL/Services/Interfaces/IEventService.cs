using Seatsure.BLL.DTOs.Common;
using Seatsure.BLL.DTOs.Events;

namespace Seatsure.BLL.Services.Interfaces;

public interface IEventService
{
    Task<PagedResult<EventDto>> GetPublishedAsync(int page, int pageSize);
    Task<EventDetailDto> GetByIdAsync(Guid id);
    Task<EventDto> CreateAsync(Guid organizerId, CreateEventRequest request);
    Task<EventDto> PublishAsync(Guid eventId, Guid organizerId);
}
