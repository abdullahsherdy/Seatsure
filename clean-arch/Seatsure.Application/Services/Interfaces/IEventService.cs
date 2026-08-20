using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Seatsure.Application.Bll.DTOs;
using Seatsure.Application.DTOs.Event;
using Seatsure.Domain;
/*
 * 
 * 
### 8.2 `IEventService`
using Seatsure.Applica
- **List published:** validate paging (`page ≥ 1`, `pageSize` within a sane bound); 
call the repository's published-events query; map to the paged-result DTO.


- **Get by id:** fetch; if missing, throw not-found; map to output DTO.


- **Create event (organizer only):** the caller's user id and role come in as a parameter (the controller will supply them from the token later — the service takes them as arguments, it does not read `HttpContext`).
    Validate the input; create the `Event` with `Status = Draft`, `OrganizerId = caller`, `CreatedAtUtc` in UTC; 
    stage and commit; 
    return the created event.

- **Publish event (organizer + owner only):** fetch the event (not-found if missing); 
verify the caller **owns** it (ownership exception if not — this is an authorization check, distinct from "is an organizer");
transition `Status` to `Published`; commit. 
Be able to explain why ownership is checked here in code and cannot be expressed by a role attribute alone.

 */
namespace Seatsure.Application.Services.Interfaces;
public  interface IEventService
{

    // list published events with paging, return a paged result of EventDetailsDto 
    // pagined result 
    Task<PagedResult<EventDto>> ListPublishedEventsAsync(int page, int pageSize);


    Task<EventDetailsDto> GetEventByIdAsync(Guid id); 

    // using id, i can find anything about the user
    Task<EventDto> CreateEventAsync(Guid CallerId,  CreateEventDto request);

    Task<EventDto> PublishEventAsync(Guid CallerId, Guid EventId);

}

