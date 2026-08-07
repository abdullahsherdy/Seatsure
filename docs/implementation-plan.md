# SeatSure — Implementation Plan

## Project Structure

Add one new project: **`Seatsure.Application`**

```
Seatsure.Domain        ← entities, enums (done)
Seatsure.DAL           ← DbContext, migrations, repository implementations
Seatsure.Application   ← NEW: repository interfaces, service interfaces, services, DTOs
Seatsure               ← API: controllers, hub, background service, Program.cs
```

Dependency chain (no circular deps):

```
Seatsure → Seatsure.Application → Seatsure.Domain
Seatsure.DAL → Seatsure.Application → Seatsure.Domain
Seatsure → Seatsure.DAL
```

---

## Phase 1 — Repository Pattern

**New project:** `Seatsure.Application`

**Repository interfaces** in `Seatsure.Application/Repositories/`:
```
IUserRepository        → GetByEmail, GetById, Add
IEventRepository       → GetById, GetPublished (paged), Add, Update
ITicketTypeRepository  → GetById, GetByEventId, Add, Update
IReservationRepository → GetById, GetByUserId, GetExpiredHolds, Add, Update
```

**Repository implementations** in `Seatsure.DAL/Repositories/`:
Each class implements its interface and takes `AppDbContext` via constructor injection.

**Teaching point:** Why the interface lives in Application and the implementation in DAL — the service layer depends on the abstraction, not the EF Core implementation. Swap EF for Dapper tomorrow without touching a single service.

---

## Phase 2 — DTOs

In `Seatsure.Application/DTOs/`:

```
Requests:  RegisterRequest, LoginRequest
           CreateEventRequest, PublishEventRequest
           CreateTicketTypeRequest
           CreateReservationRequest

Responses: AuthResponse (token, expiresAtUtc)
           EventResponse, EventListResponse (paged envelope)
           TicketTypeResponse
           ReservationResponse
```

All DTOs are `record` types. Manual mapping methods (no AutoMapper).

**Teaching point:** Entities never cross the controller boundary. DTOs decouple the API contract from the DB schema.

---

## Phase 3 — Service Layer

In `Seatsure.Application/Services/`:

```
IAuthService          → Register, Login
IEventService         → Create, GetById, GetPublished, Publish
ITicketTypeService    → Add, GetByEvent
IReservationService   → CreateHold, Confirm, Cancel, GetUserReservations
```

Implementations take repository interfaces (not DbContext) via constructor injection.

**Teaching point:** Business rules live here, not in controllers or repositories. One responsibility per service.

---

## Phase 4 — JWT Authentication

**Packages to add to `Seatsure` (API):**
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `BCrypt.Net-Next`

**`AuthService` implementation:**
- `Register` → hash password with BCrypt, persist via `IUserRepository`, return token
- `Login` → verify hash, issue JWT with claims: `sub` (userId), `role`, `email`

**`Program.cs` wiring:**
- Add JWT Bearer middleware
- Configure `appsettings.json` with `Jwt:Key`, `Jwt:Issuer`, `Jwt:ExpiresInMinutes`

**Teaching point:** authn (are you who you say you are?) vs authz (are you allowed?) vs ownership (do you own this resource?). `[Authorize(Roles = "Organizer")]` handles authz; ownership is a manual check in the service.

---

## Phase 5 — Controllers

In `Seatsure/Controllers/`:

```
AuthController         → POST /api/auth/register
                         POST /api/auth/login

EventsController       → GET  /api/events
                         GET  /api/events/{id}
                         POST /api/events
                         POST /api/events/{id}/publish

TicketTypesController  → GET  /api/events/{id}/ticket-types
                         POST /api/events/{id}/ticket-types

ReservationsController → POST /api/ticket-types/{id}/reservations
                         POST /api/reservations/{id}/confirm
                         POST /api/reservations/{id}/cancel
                         GET  /api/users/me/reservations
```

**Teaching point:** Thin controllers — validate input, call service, map to response, return status code. No business logic here.

---

## Phase 6 — Concurrency Deep Dive (core lesson)

Inside `ReservationService.CreateHold`:

```
1. Load TicketType (EF tracks RowVersion automatically)
2. Check AvailableQuantity >= requested quantity
3. Decrement AvailableQuantity, create Reservation with Status=Pending
4. SaveChanges → EF adds RowVersion to WHERE clause
5. DbUpdateConcurrencyException → catch → return 409 Problem Details
```

**Demo:** two terminal windows firing `POST /api/ticket-types/{id}/reservations` simultaneously for the last ticket. One gets `201`, one gets `409`.

**Teaching point:** Why you can't just check-then-write in two steps without the concurrency token. This is the load-bearing lesson of the whole project.

---

## Phase 7 — SignalR Hub

In `Seatsure/Hubs/EventAvailabilityHub.cs`:

```
JoinEvent(eventId) → adds caller to group "event-{eventId}"
// server-to-client:
AvailabilityChanged(ticketTypeId, availableQuantity)
```

`ReservationService` gets `IHubContext<EventAvailabilityHub>` injected and broadcasts after every hold create/confirm/cancel.

**Teaching point:** REST for commands (write operations), SignalR for push notifications (state changes). No auth required to receive availability updates.

---

## Phase 8 — Background Service

`HoldExpiryService : BackgroundService` in `Seatsure/`:

```
Every 30s:
  → query Reservations where Status==Pending && HoldExpiresAtUtc < UtcNow
  → set Status=Expired, restore AvailableQuantity, broadcast AvailabilityChanged
```

**Teaching point:** `BackgroundService` is a singleton — it cannot directly inject scoped services like `AppDbContext`. Must use `IServiceScopeFactory` to create a scope per scan.

---

## Phase 9 — Tests (xUnit)

New project: `Seatsure.Tests`

**Unit tests** on `ReservationService` (mock repositories):
- Happy path: hold created, quantity decremented
- Oversell: concurrency conflict → 409
- Expiry: expired hold restores inventory

**Integration tests** (`WebApplicationFactory`):
- Full flow: register → create event → add ticket type → reserve → confirm
- Negative: reserve more than available → 409

---

## Summary

| Phase | Deliverable | Key Lesson |
|---|---|---|
| 1 | Repository interfaces + implementations | Interface segregation, DI |
| 2 | DTOs (records) | API/DB decoupling |
| 3 | Service layer | Business rules ownership |
| 4 | JWT Auth | authn vs authz vs ownership |
| 5 | All 4 controllers | Thin controllers |
| 6 | Concurrency + 409 demo | RowVersion, optimistic locking |
| 7 | SignalR hub | Real-time push |
| 8 | HoldExpiryService | `IServiceScopeFactory` gotcha |
| 9 | Unit + integration tests | Proving concurrency logic |
