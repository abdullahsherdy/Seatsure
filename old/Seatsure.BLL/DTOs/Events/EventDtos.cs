namespace Seatsure.BLL.DTOs.Events;

public record CreateEventRequest(string Title, string Description, string VenueName, DateTime StartsAtUtc);

public record EventDto(
    Guid Id,
    Guid OrganizerId,
    string Title,
    string Description,
    string VenueName,
    DateTime StartsAtUtc,
    string Status,
    DateTime CreatedAtUtc);

/// <summary>Event with its ticket types, returned by GET /api/events/{id}.</summary>
public record EventDetailDto(
    Guid Id,
    Guid OrganizerId,
    string Title,
    string Description,
    string VenueName,
    DateTime StartsAtUtc,
    string Status,
    DateTime CreatedAtUtc,
    IEnumerable<TicketTypes.TicketTypeDto> TicketTypes);
