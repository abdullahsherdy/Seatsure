using Seatsure.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Seatsure.Application.Interfaces;
using Seatsure.Domain;
using Microsoft.EntityFrameworkCore;

namespace Seatsure.Infrastructure.Repositories;

public class EventRepository : IEventRepository
{

    private readonly AppDbContext _context;

    public EventRepository(AppDbContext context) => _context = context;

    public async Task AddAsync(Event _event)
    {
        await _context.Events.AddAsync(_event);
    }

    public async Task<Event?> GetByIdAsync(Guid id) => await _context.Events.FindAsync(id); 
    public async Task<(IEnumerable<Event> events, int Totalcount)> GetPublishedAsync(int page, int pageSize)
    {
        // where not async 
        var query =  _context.Events.Where(e => e.Status == EventStatus.Published);

        // error here before, because of missing async in function header 
        var totalCount = await query.CountAsync();

        var events = await query
            .OrderBy(e => e.StartsAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(); 

        return (events, totalCount);
        
    }
}
