using Seatsure.BLL.DTOs.Reservations;

namespace Seatsure.BLL.Services.Interfaces;

public interface IReservationService
{
    /// <summary>Places a pending hold, decrementing available inventory under optimistic concurrency.</summary>
    Task<ReservationDto> CreateHoldAsync(Guid ticketTypeId, Guid userId, CreateReservationRequest request);

    /// <summary>Confirms a pending hold owned by the user.</summary>
    Task<ReservationDto> ConfirmAsync(Guid reservationId, Guid userId);

    /// <summary>Cancels a hold/confirmation owned by the user and restores inventory.</summary>
    Task<ReservationDto> CancelAsync(Guid reservationId, Guid userId);

    Task<IEnumerable<ReservationDto>> GetByUserAsync(Guid userId); // get user with his reservations 

    /// <summary>Expires all overdue pending holds and restores their inventory. Returns the number expired.</summary>
    Task<int> ExpireHoldsAsync();
}
