using Microsoft.EntityFrameworkCore;
using Seatsure.DAL.Repositories.Interfaces;
using Seatsure.Domain;

namespace Seatsure.DAL.Repositories.Impl;

public class TicketTypeRepository : ITicketTypeRepository
{
    private readonly AppDbContext _context;

    public TicketTypeRepository(AppDbContext context) => _context = context;

    public async Task<TicketType?> GetByIdAsync(Guid id) =>
        await _context.TicketTypes.FindAsync(id);


    public async Task<IEnumerable<TicketType>> GetByEventIdAsync(Guid eventId) =>
        await _context.TicketTypes
            .Where(t => t.EventId == eventId)
            .ToListAsync();
            

    public async Task AddAsync(TicketType ticketType) =>
        await _context.TicketTypes.AddAsync(ticketType);
}
