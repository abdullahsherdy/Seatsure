namespace Seatsure.BLL.DTOs.Reservations;

public record CreateReservationRequest(int Quantity);
// reservation, user must be logged in, from cookies


public record ReservationDto(
    Guid Id,
    Guid TicketTypeId,
    Guid UserId,
    int Quantity,
    string Status,
    DateTime HoldExpiresAtUtc,
    DateTime CreatedAtUtc,
    DateTime? ConfirmedAtUtc);
