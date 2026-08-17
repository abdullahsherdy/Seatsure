namespace Seatsure.BLL.DTOs.TicketTypes;

public record CreateTicketTypeRequest(string Name, decimal Price, int TotalQuantity);

public record TicketTypeDto(
    Guid Id,
    Guid EventId,
    string Name,
    decimal Price,
    int TotalQuantity,
    int AvailableQuantity);
