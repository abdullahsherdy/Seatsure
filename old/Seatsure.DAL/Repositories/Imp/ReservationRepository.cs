using Microsoft.EntityFrameworkCore;
using Seatsure.DAL.Repositories.Interfaces;
using Seatsure.Domain;

namespace Seatsure.DAL.Repositories.Impl;

public class ReservationRepository : IReservationRepository
{
    private readonly AppDbContext _context;

    public ReservationRepository(AppDbContext context) => _context = context;

    public async Task<Reservation?> GetByIdAsync(Guid id) =>
        await _context.Reservations
            .Include(r => r.TicketType)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<IEnumerable<Reservation>> GetByUserIdAsync(Guid userId) =>
        await _context.Reservations
            .Where(r => r.UserId == userId)
            .ToListAsync();

    public async Task<IEnumerable<Reservation>> GetExpiredHoldsAsync() =>
        await _context.Reservations
            .Include(r => r.TicketType)
            .Where(r => r.Status == ReservationStatus.Pending && r.HoldExpiresAtUtc < DateTime.UtcNow)
            .ToListAsync();

    public async Task AddAsync(Reservation reservation) =>
        await _context.Reservations.AddAsync(reservation);
}
