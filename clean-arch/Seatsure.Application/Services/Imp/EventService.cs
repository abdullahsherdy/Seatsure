using Seatsure.Application.Bll.DTOs;
using Seatsure.Application.DTOs;
using Seatsure.Application.Interfaces;
using Seatsure.Application.Services.Interfaces;
using Seatsure.Application.Exceptions;
using Seatsure.Domain;
using Seatsure.Application.DTOs.TicketTypes;
using System.Net.Http.Headers;
namespace Seatsure.Application.Services.Imp;


internal sealed class EventService : IEventService
{
    private const int MAxPageSize = 100;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventRepository _eventRepository;
    private readonly IUserRepository _userRepository;

    public EventService(IUnitOfWork unitOfWork, IEventRepository eventRepository, IUserRepository userRepository)
    {
        _unitOfWork = unitOfWork;
        _eventRepository = eventRepository;
        _userRepository = userRepository;
    }

    public async Task<EventDto> CreateEventAsync(Guid CallerId, CreateEventDto request)
    {
        // string validation 

        if (string.IsNullOrEmpty(request.title))
            throw new ValidationException("Title is Required"); 

        if (string.IsNullOrEmpty(request.venue))
            throw new ValidationException("Description is Required");

        if (request.startsAtutc <= DateTime.UtcNow)
            throw new ValidationException("Date must be in the future");

        var organizer = await _userRepository.GetByIdAsync(CallerId) ?? throw new NotFoundException("user Not Found");


        // allowed users to created events -> organizers 
        // authorized to create events or not
        /// authorization done using roles
        /// 

        if (organizer.Role != UserRole.Organizer)
            throw new ForbiddenException("Only Organizers can create events");

        //if (organizer.Role == UserRole.Organizer)
        //    {
        //    // authorized to create events or not
        //    /// authorization done using roles
        //    /// 
        //}else
        //{ throw}
        // mapping 

        var ev = new Event
        {
            OrganizerId = CallerId,
            Title = request.title, 
            Description = request.description, 
            VenueName = request.venue, 
            StartsAtUtc = request.startsAtutc, 
            Status = EventStatus.Draft, 
            CreatedAtUtc = DateTime.UtcNow

        };

        await _eventRepository.AddAsync(ev);

        await _unitOfWork.SaveChangesAsync();

        return new EventDto(ev.Id, ev.OrganizerId, ev.Title, ev.Description, ev.VenueName, ev.StartsAtUtc, ev.Status); 

    }

    public async Task<EventDetailsDto> GetEventByIdAsync(Guid id)
    {
      var ev = await _eventRepository.GetByIdAsync(id) ?? throw new NotFoundException("Event Not Found");

        return new EventDetailsDto(
                                    ev.Id, ev.OrganizerId, 
                                    ev.Title,
                                    ev.Description,
                                    ev.VenueName, 
                                    ev.StartsAtUtc, 
                                    ev.Status, 
                                    ev.TicketTypes.Select(
                                            t => new TicketTypeDto(t.Id, 
                                            t.EventId,
                                            t.Name,
                                            t.Price,
                                            t.TotalQuantity,
                                            t.AvailableQuantity))
                                            .ToList()
                                    );
    }

    public async Task<PagedResult<EventDto>> ListPublishedEventsAsync(int page, int pageSize)
    {
        if (page < 1) throw new ValidationException("number of pages can't be less than 1");
        if(pageSize < 1 || pageSize > MAxPageSize) throw new ValidationException($"page size must be between 1 and {MAxPageSize}");


        var (items, totalCount) = await _eventRepository.GetPublishedAsync(page, pageSize); 

        throw new NotImplementedException();
    }

    public Task<EventDto> PublishEventAsync(Guid CallerId, Guid EventId)
    {
        throw new NotImplementedException();
    }
}