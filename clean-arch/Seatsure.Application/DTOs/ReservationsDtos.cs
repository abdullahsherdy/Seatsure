namespace Seatsure.Application.DTOs.Reservation;
using Seatsure.Domain; 
// input for post/reservation endpoint 
// id is passed from cookies for user, Event, ticket 

public record CreateReservationDto(int quantity);

public record ReservationDto(
                            Guid Id, 
                            Guid UserId, 
                            Guid TicketId, 
                            int quantity,
                            ReservationStatus status, 
                            DateTime HoldExpriesAtUtc,
                            DateTime CreatedAtutc,
                            DateTime? ConfirmedAtUtc);