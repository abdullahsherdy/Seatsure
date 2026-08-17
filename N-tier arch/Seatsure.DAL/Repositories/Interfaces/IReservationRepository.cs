using Seatsure.Domain;

namespace Seatsure.DAL.Repositories.Interfaces;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(Guid id);
    Task<IEnumerable<Reservation>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<Reservation>> GetExpiredHoldsAsync();
    Task AddAsync(Reservation reservation);
}
