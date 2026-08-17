using Seatsure.BLL.DTOs.Events;
using Seatsure.BLL.DTOs.Reservations;
using Seatsure.BLL.DTOs.TicketTypes;
using Seatsure.Domain;

namespace Seatsure.BLL.Services;

/// <summary>Single source of truth for entity → DTO projection, keeping services DRY.</summary>
internal static class Mappings
{
    public static TicketTypeDto ToDto(this TicketType t) =>
        new(t.Id, t.EventId, t.Name, t.Price, t.TotalQuantity, t.AvailableQuantity);

    public static EventDto ToDto(this Event e) =>
        new(e.Id, e.OrganizerId, e.Title, e.Description, e.VenueName, e.StartsAtUtc, e.Status.ToString(), e.CreatedAtUtc);

    public static EventDetailDto ToDetailDto(this Event e) =>
        new(e.Id, e.OrganizerId, e.Title, e.Description, e.VenueName, e.StartsAtUtc, e.Status.ToString(),
            e.CreatedAtUtc, e.TicketTypes.Select(ToDto));

    public static ReservationDto ToDto(this Reservation r) =>
        new(r.Id, r.TicketTypeId, r.UserId, r.Quantity, r.Status.ToString(),
            r.HoldExpiresAtUtc, r.CreatedAtUtc, r.ConfirmedAtUtc);
}
