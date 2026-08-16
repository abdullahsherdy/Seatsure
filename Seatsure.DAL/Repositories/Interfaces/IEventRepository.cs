using Seatsure.Domain;

namespace Seatsure.DAL.Repositories.Interfaces;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id);
    Task<(IEnumerable<Event> Items, int TotalCount)> GetPublishedAsync(int page, int pageSize);
    Task AddAsync(Event ev);
}
