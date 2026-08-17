using Seatsure.BLL.DTOs.TicketTypes;
using Seatsure.BLL.Exceptions;
using Seatsure.BLL.Services.Interfaces;
using Seatsure.DAL;
using Seatsure.DAL.Repositories.Interfaces;
using Seatsure.Domain;

namespace Seatsure.BLL.Services;

internal sealed class TicketTypeService : ITicketTypeService
{
    private readonly ITicketTypeRepository _ticketTypes;
    private readonly IEventRepository _events;
    private readonly IUnitOfWork _unitOfWork;

    public TicketTypeService(ITicketTypeRepository ticketTypes, IEventRepository events, IUnitOfWork unitOfWork)
    {
        _ticketTypes = ticketTypes;
        _events = events;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<TicketTypeDto>> GetByEventIdAsync(Guid eventId)
    {
        _ = await _events.GetByIdAsync(eventId)
            ?? throw new NotFoundException($"Event {eventId} was not found.");

        var ticketTypes = await _ticketTypes.GetByEventIdAsync(eventId);
        return ticketTypes.Select(t => t.ToDto());
    }

    public async Task<TicketTypeDto> AddAsync(Guid eventId, Guid organizerId, CreateTicketTypeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Name is required.");
        if (request.Price < 0)
            throw new ValidationException("Price cannot be negative.");
        if (request.TotalQuantity < 1)
            throw new ValidationException("TotalQuantity must be at least 1.");

        var ev = await _events.GetByIdAsync(eventId)
            ?? throw new NotFoundException($"Event {eventId} was not found.");

        // Ownership check — only the owning organizer may add ticket types (README §3.3).
        if (ev.OrganizerId != organizerId)
            throw new ForbiddenException("You can only add ticket types to events you own.");

        var ticketType = new TicketType
        {
            EventId = eventId,
            Name = request.Name.Trim(),
            Price = request.Price,
            TotalQuantity = request.TotalQuantity,
            AvailableQuantity = request.TotalQuantity // starts fully available
        };

        await _ticketTypes.AddAsync(ticketType);
        await _unitOfWork.SaveChangesAsync();

        return ticketType.ToDto();
    }
}
