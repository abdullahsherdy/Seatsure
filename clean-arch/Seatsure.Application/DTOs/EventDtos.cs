using Seatsure.Domain; 

namespace Seatsure.Application.DTOs;

//  use it for update or delete (input) for put or delete 
public record EventDto(Guid id, Guid OrganizerId, string title, string description, string venue, DateTime startsAtutc, EventStatus status);

// input for post/event endpoint 
public record CreateEventDto(Guid organizerId, string title, string description, string venue, DateTime startsAtutc);


// output record for frontend to display event details, and also for authorization purposes. for Get/event/{id} endpoint
// point to fix, change TicketType to ticketTypeDto. 

public record EventDetailsDto(Guid Id, Guid OrganizerId, string Title, string description, string venue, DateTime startsAtutc, EventStatus status, IEnumerable<TicketTypes.TicketTypeDto> tickets);