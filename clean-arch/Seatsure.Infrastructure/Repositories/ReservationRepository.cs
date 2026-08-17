using Seatsure.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Seatsure.Domain;
using Seatsure.Infrastructure.Data;
using Microsoft.Identity.Client;
using Microsoft.EntityFrameworkCore;
namespace Seatsure.Infrastructure.Repositories;
public class ReservationRepository:IReservationRepistory
{
    private readonly AppDbContext _context; 
    
    public ReservationRepository(AppDbContext context) => _context = context;

    public async Task AddAsync(Reservation reservation)
    {
        await _context.Reservations.AddAsync(reservation);
    }

    public async Task<Reservation?> GetByIdAsync(Guid id) => await _context.Reservations.FindAsync(id);


    public async Task<IEnumerable<Reservation>> GetByUserIdAsync(Guid userId) => await _context.Reservations.Where(r => r.UserId == userId).ToListAsync();


    public async Task<IEnumerable<Reservation>> GetExpiredHoldAsync() => await 
        _context.Reservations
        .Include(r => r.TicketType)
        .Where(r => r.Status == ReservationStatus.Pending 
        && r.HoldExpiresAtUtc < DateTime.UtcNow)
        .ToListAsync();
}
