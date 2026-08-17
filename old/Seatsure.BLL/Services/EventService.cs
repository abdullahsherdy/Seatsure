using Seatsure.BLL.DTOs.Common;
using Seatsure.BLL.DTOs.Events;
using Seatsure.BLL.Exceptions;
using Seatsure.BLL.Services.Interfaces;
using Seatsure.DAL;
using Seatsure.DAL.Repositories.Interfaces;
using Seatsure.Domain;

namespace Seatsure.BLL.Services;

internal sealed class EventService : IEventService
{
    private const int MaxPageSize = 100;
    private readonly IEventRepository _events;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    public EventService(IEventRepository events, IUserRepository users, IUnitOfWork unitOfWork)
    {
        _events = events;
        _users = users;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<EventDto>> GetPublishedAsync(int page, int pageSize)
    {
        if (page < 1) throw new ValidationException("page must be 1 or greater.");
        if (pageSize < 1 || pageSize > MaxPageSize)
            throw new ValidationException($"pageSize must be between 1 and {MaxPageSize}.");

        var (items, total) = await _events.GetPublishedAsync(page, pageSize);
        return new PagedResult<EventDto>(items.Select(e => e.ToDto()), page, pageSize, total);
    }

    public async Task<EventDetailDto> GetByIdAsync(Guid id)
    {
        var ev = await _events.GetByIdAsync(id)
            ?? throw new NotFoundException($"Event {id} was not found.");
        return ev.ToDetailDto();
    }

    public async Task<EventDto> CreateAsync(Guid organizerId, CreateEventRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ValidationException("Title is required.");
        if (string.IsNullOrWhiteSpace(request.VenueName))
            throw new ValidationException("VenueName is required.");
        if (request.StartsAtUtc <= DateTime.UtcNow)
            throw new ValidationException("StartsAtUtc must be in the future.");

        // Guard against a valid JWT whose user no longer exists.
        var organizer = await _users.GetByIdAsync(organizerId)
            ?? throw new NotFoundException("Organizer account was not found.");
        if (organizer.Role != UserRole.Organizer)
            throw new ForbiddenException("Only organizers can create events.");

        var ev = new Event
        {
            OrganizerId = organizerId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            VenueName = request.VenueName.Trim(),
            StartsAtUtc = request.StartsAtUtc,
            Status = EventStatus.Draft,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _events.AddAsync(ev);
        await _unitOfWork.SaveChangesAsync();

        return ev.ToDto();
    }

    public async Task<EventDto> PublishAsync(Guid eventId, Guid organizerId)
    {
        var ev = await _events.GetByIdAsync(eventId)
            ?? throw new NotFoundException($"Event {eventId} was not found.");

        // Ownership check — authz beyond role (README §6).
        if (ev.OrganizerId != organizerId)
            throw new ForbiddenException("You can only publish events you own.");

        if (ev.Status == EventStatus.Cancelled)
            throw new ConflictException("A cancelled event cannot be published.");

        // Idempotent: republishing an already-published event is a no-op success.
        if (ev.Status != EventStatus.Published)
        {
            ev.Status = EventStatus.Published;
            await _unitOfWork.SaveChangesAsync();
        }

        return ev.ToDto();
    }
}
