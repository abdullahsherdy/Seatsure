namespace Seatsure.Application.DTOs.TicketTypes;

// input for post/ticketType endpoint
// auto-generated 

public record CreateTicketTypeDto(Guid EventId, string name, decimal price, int TotalQuantity); 


// output for Get/ticketType/{id} endpoint, to display ticket type details, and also for authorization purposes.
public record TicketTypeDto(Guid Id, Guid EventId, string name, decimal price, int TotlalQuantity, int RemainingQuantity); 