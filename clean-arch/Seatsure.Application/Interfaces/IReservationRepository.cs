using Seatsure.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Seatsure.Application.Interfaces;

public interface IReservationRepository
{

    Task<Reservation?> GetByIdAsync(Guid id);

    Task<IEnumerable<Reservation>> GetByUserIdAsync(Guid userId);

    Task<IEnumerable<Reservation>> GetExpiredHoldAsync();

    Task AddAsync(Reservation reservation);
}
