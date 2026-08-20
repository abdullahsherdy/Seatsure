using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Seatsure.Application.Exceptions; 
using Seatsure.Application.Services.Interfaces;
using Seatsure.Application.Interfaces;
using Seatsure.Application.DTOs.TicketTypes;
using Seatsure.Domain;


namespace Seatsure.Application.Services.Imp;

public sealed class TicektTypeService : ITicketTypeService
{

     private readonly IUnitOfWork _unitOfWork;
     private readonly ITicketTypeRepository _ticketTypeRepository;
     private readonly IEventRepository _eventRepository;

    public TicektTypeService(IUnitOfWork unitOfWork, ITicketTypeRepository ticketTypeRepository, IEventRepository eventRepository)
    {
        _unitOfWork = unitOfWork;
        _ticketTypeRepository = ticketTypeRepository;
        _eventRepository = eventRepository;
    }

    public async Task<TicketTypeDto> AddAsync(Guid CallerId, Guid eventId, CreateTicketTypeDto request)
    {

        // callerId, EventId, request.name, request.price, request.TotalQuantity
        // valdiate strings 

        if(string.IsNullOrEmpty(request.name))
            throw new ValidationException("Ticket type name is required.");

        if(request.price < 1)
            throw new ValidationException("price can't be lass than 1")

        if (request.TotalQuantity < 0)
            throw new ValidationException("Total quantity can't be less than zero.");


        var ev = await _eventRepository.GetByIdAsync(eventId) ?? throw new NotFoundException($"Event with id {eventId} not found.");

        // why i store organizerId in the event table, while i store the whole entity 
       

        if (ev.OrganizerId != CallerId)
            throw new ValidationException("You are not authorized to add ticket types to this event.");

        var ticket = new TicketType
        {
            EventId = eventId,
            Name = request.name,
            Price = request.price,
            TotalQuantity = request.TotalQuantity,
            AvailableQuantity = request.TotalQuantity,
        };

        // add 
        await _ticketTypeRepository.AddAsync(ticket);
        await _unitOfWork.SaveChangesAsync();

        return new TicketTypeDto(ticket.Id, ticket.EventId, ticket.Name, ticket.Price, ticket.TotalQuantity, ticket.AvailableQuantity);
    }

    public async Task<IEnumerable<TicketTypeDto>> ListTicketTypesForEventAsync(Guid eventId)
    {   
        // event exist or not 
        var ev = await _eventRepository.GetByIdAsync(eventId) ?? throw new NotFoundException($"Event with id {eventId} not found.");

        var tickets = await _ticketTypeRepository.GetByEventIdAsync(eventId);

        return tickets.Select(t => new TicketTypeDto(t.Id, t.EventId, t.Name, t.Price, t.TotalQuantity, t.AvailableQuantity)).ToList();
    }
}
