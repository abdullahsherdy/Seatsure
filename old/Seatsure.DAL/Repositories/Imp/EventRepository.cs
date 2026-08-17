using Microsoft.EntityFrameworkCore;
using Seatsure.DAL.Repositories.Interfaces;
using Seatsure.Domain;

namespace Seatsure.DAL.Repositories.Impl;

public class EventRepository : IEventRepository
{
    private readonly AppDbContext _context;

    public EventRepository(AppDbContext context) => _context = context;

    public async Task<Event?> GetByIdAsync(Guid id) =>
        await _context.Events
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.Id == id);

    public async Task<(IEnumerable<Event> Items, int TotalCount)> GetPublishedAsync(int page, int pageSize)
    {
        var query = _context.Events.Where(e => e.Status == EventStatus.Published);

        var total = await query.CountAsync();
        
        var items = await query
            .OrderBy(e => e.StartsAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, total);
    }

    public async Task AddAsync(Event ev) =>
        await _context.Events.AddAsync(ev);
}
