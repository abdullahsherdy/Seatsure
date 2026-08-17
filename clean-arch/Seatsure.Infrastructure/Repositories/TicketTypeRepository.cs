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

public class TicketTypeRepository: ITickeTypeRepository
{
    private readonly AppDbContext _context;

    public TicketTypeRepository(AppDbContext context) => _context = context;

    public async Task AddAsync(TicketType ticketType) => await _context.TicketTypes.AddAsync(ticketType);

    public async Task<IEnumerable<TicketType>> GetByEventIdAsync(Guid eventId) => await _context.TicketTypes
            .Where(t => t.EventId == eventId)
            .ToListAsync();

    public async Task<TicketType?> GetTicketTypeByIdAsync(Guid id) => await _context.TicketTypes.FindAsync(id);
    
}
