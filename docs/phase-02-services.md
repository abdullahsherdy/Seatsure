# Phase 2 — Service Layer (BLL)

## Context

Phase 1 delivered the repository layer inside `Seatsure.DAL` (interfaces in
`Repositories/Interfaces/`, implementations in `Repositories/Imp/`, all registered in DI).

Phase 2 builds the **Business Logic Layer** — the one place where business rules live.
It owns DTOs, service contracts + implementations, password hashing, and JWT token
issuing. The core deliverable is `ReservationService`, where the optimistic-concurrency
handling (`RowVersion` → `409`) is implemented — the load-bearing lesson of the project
(README §4).

Scope is BLL only. Controllers, JWT Bearer *validation* middleware, `[Authorize]`,
SignalR, the `HoldExpiryService` host, and tests are Phase 3+.

## Architectural decision recap

We removed `Seatsure.Application`. Repository interfaces already live in `DAL`. Service
interfaces, service implementations, and DTOs go in a new **`Seatsure.BLL`** project.

```
Seatsure.Domain   ← entities, enums (done)
Seatsure.DAL      ← DbContext, migrations, repositories (done)
Seatsure.BLL      ← NEW: DTOs, services, auth helpers (this phase)
Seatsure          ← API: controllers, hub, background service (later)
```

Dependency chain (no cycles):

```
Seatsure → Seatsure.BLL → Seatsure.DAL → Seatsure.Domain
```

Trade-off: because the repository interfaces live in DAL, `BLL → DAL` means BLL
transitively references EF Core. That is acceptable here and matches README §4's intent
that the *service* catches `DbUpdateConcurrencyException`. A stricter onion split would put
the interfaces in a separate abstractions project; we deliberately chose the simpler
four-layer hierarchy.

---

## Step 1 — Create `Seatsure.BLL` project

```bash
dotnet new classlib -n Seatsure.BLL -f net8.0
dotnet sln add Seatsure.BLL/Seatsure.BLL.csproj
```

Delete the auto-generated `Class1.cs`. Create the folder structure:

```
Seatsure.BLL/
├── DTOs/
├── Exceptions/
├── Security/
└── Services/
    ├── Interfaces/
    └── Imp/
```

Confirm the `.csproj` has nullable enabled (matches DAL):

```xml
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
  <ImplicitUsings>enable</ImplicitUsings>
  <Nullable>enable</Nullable>
</PropertyGroup>
```

---

## Step 2 — References and packages

```bash
# BLL references DAL (for repo interfaces) and Domain (for entities)
dotnet add Seatsure.BLL/Seatsure.BLL.csproj reference Seatsure.DAL/Seatsure.DAL.csproj
dotnet add Seatsure.BLL/Seatsure.BLL.csproj reference Seatsure.Domain/Seatsure.Domain.csproj

# API references BLL (in addition to its existing DAL/Domain refs)
dotnet add Seatsure/Seatsure.csproj reference Seatsure.BLL/Seatsure.BLL.csproj

# Packages (pinned — versions resolve to latest stable 8.x)
dotnet add Seatsure.BLL/Seatsure.BLL.csproj package BCrypt.Net-Next
dotnet add Seatsure.BLL/Seatsure.BLL.csproj package System.IdentityModel.Tokens.Jwt
dotnet add Seatsure.BLL/Seatsure.BLL.csproj package Microsoft.Extensions.Options
```

- `BCrypt.Net-Next` — password hashing (pinning the hasher here, per README §6).
- `System.IdentityModel.Tokens.Jwt` — `JwtSecurityTokenHandler` for issuing tokens; pulls
  in `Microsoft.IdentityModel.Tokens` (`SymmetricSecurityKey`, `SigningCredentials`).
- `Microsoft.Extensions.Options` — lets `TokenService` receive `IOptions<JwtSettings>`.
- `DbUpdateConcurrencyException` is available transitively via the DAL reference — no
  explicit EF Core package needed in BLL.

**Teaching point:** the Bearer *validation* middleware (`Microsoft.AspNetCore.Authentication.JwtBearer`)
belongs in the API project, not BLL. BLL only *issues* tokens; the API *validates* them.

---

## Step 3 — DTOs (`Seatsure.BLL/DTOs/`)

All DTOs are `record` types, mapped manually (no AutoMapper). Entities never cross the
service boundary — services take request DTOs and return response DTOs.

```csharp
namespace Seatsure.BLL.DTOs;

// Auth
public record RegisterRequest(string Name, string Email, string Password, string Role);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, DateTime ExpiresAtUtc);

// Events
public record CreateEventRequest(string Title, string Description, string VenueName, DateTime StartsAtUtc);
public record EventResponse(Guid Id, string Title, string Description, string VenueName, DateTime StartsAtUtc, string Status);
public record EventDetailResponse(Guid Id, string Title, string Description, string VenueName, DateTime StartsAtUtc, string Status, IEnumerable<TicketTypeResponse> TicketTypes);

// Ticket types
public record CreateTicketTypeRequest(string Name, decimal Price, int TotalQuantity);
public record TicketTypeResponse(Guid Id, string Name, decimal Price, int TotalQuantity, int AvailableQuantity);

// Reservations
public record CreateReservationRequest(int Quantity);
public record ReservationResponse(Guid Id, Guid TicketTypeId, int Quantity, string Status, DateTime HoldExpiresAtUtc, DateTime CreatedAtUtc, DateTime? ConfirmedAtUtc);

// Generic paged envelope (README §3.5)
public record PagedResponse<T>(IEnumerable<T> Items, int Page, int PageSize, int TotalCount);
```

`Status` fields are strings in responses (`enum.ToString()`) to keep the JSON contract
stable and readable. `Role` in `RegisterRequest` is a string, parsed against `UserRole` in
`AuthService`.

---

## Step 4 — Domain exceptions (`Seatsure.BLL/Exceptions/`)

Services throw typed exceptions; the controller layer (Phase 3) maps them to HTTP status
codes via an exception-handling middleware. This keeps services free of `IActionResult` /
HTTP concerns.

```csharp
namespace Seatsure.BLL.Exceptions;

public class NotFoundException(string message) : Exception(message);        // → 404
public class ValidationException(string message) : Exception(message);      // → 400
public class ConflictException(string message) : Exception(message);        // → 409
public class ForbiddenException(string message) : Exception(message);       // → 403
public class UnauthorizedException(string message) : Exception(message);    // → 401
```

**Teaching point:** the concurrency conflict (`409`) and the "email taken" conflict (`409`)
are both `ConflictException`, but the messages differ — the message becomes the RFC 7807
Problem Details `detail` in Phase 3.

---

## Step 5 — Security helpers (`Seatsure.BLL/Security/`)

**JwtSettings.cs** — bound from `appsettings.json` `Jwt` section.

```csharp
namespace Seatsure.BLL.Security;

public class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiresInMinutes { get; set; } = 60;
}
```

**IPasswordHasher.cs / PasswordHasher.cs** — thin wrapper over BCrypt so services depend on
an abstraction, not the package directly.

```csharp
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);
    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
```

**ITokenService.cs / TokenService.cs** — issues a signed JWT with claims `sub`, `email`,
`role` (README §6). Receives `IOptions<JwtSettings>` and returns `(token, expiresAtUtc)`.
Signs with `SymmetricSecurityKey` + `HmacSha256`.

---

## Step 6 — Service interfaces (`Seatsure.BLL/Services/Interfaces/`)

Method signatures map 1:1 to the API contract (README §3). The authenticated caller's
`userId` / `role` is passed in by the controller (extracted from JWT claims) — services
never touch `HttpContext`.

```csharp
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
}

public interface IEventService
{
    Task<PagedResponse<EventResponse>> GetPublishedAsync(int page, int pageSize);
    Task<EventDetailResponse> GetByIdAsync(Guid id);                          // throws NotFound
    Task<EventResponse> CreateAsync(CreateEventRequest request, Guid organizerId);
    Task PublishAsync(Guid eventId, Guid organizerId);                        // ownership check
}

public interface ITicketTypeService
{
    Task<IEnumerable<TicketTypeResponse>> GetByEventIdAsync(Guid eventId);    // throws NotFound if event missing
    Task<TicketTypeResponse> AddAsync(Guid eventId, CreateTicketTypeRequest request, Guid organizerId); // ownership check
}

public interface IReservationService
{
    Task<ReservationResponse> CreateHoldAsync(Guid ticketTypeId, CreateReservationRequest request, Guid userId);
    Task<ReservationResponse> ConfirmAsync(Guid reservationId, Guid userId);
    Task<ReservationResponse> CancelAsync(Guid reservationId, Guid userId);
    Task<IEnumerable<ReservationResponse>> GetMyReservationsAsync(Guid userId);
    Task<int> ExpireHoldsAsync();   // used by HoldExpiryService (Phase 3); returns count expired
}
```

**Teaching point:** `ExpireHoldsAsync` lives on the service, not the background host. The
Phase-3 `HoldExpiryService` is a thin timer that resolves a scoped `IReservationService`
and calls this method — the *logic* stays testable in the BLL.

---

## Step 7 — Service implementations (`Seatsure.BLL/Services/Imp/`)

Constructor-inject the repository interfaces from `Seatsure.DAL.Repositories.Interfaces`
(never `AppDbContext`). All four repositories in a request share the same scoped
`AppDbContext`, so mutating a tracked entity via one repo and calling `SaveChangesAsync()`
on another persists both — EF Core's built-in Unit of Work.

### AuthService
- **Register:** `GetByEmailAsync` → if found, throw `ConflictException("Email already registered")`.
  Parse `Role` string to `UserRole` (invalid → `ValidationException`). Hash password, build
  `User`, `AddAsync` + `SaveChangesAsync`, issue token, return `AuthResponse`.
- **Login:** `GetByEmailAsync` → null or `Verify` fails → `UnauthorizedException`. Issue
  token, return `AuthResponse`.

### EventService
- **CreateAsync:** build `Event` (Status = `Draft`, `OrganizerId = organizerId`), persist, map.
- **GetByIdAsync:** repo includes `TicketTypes`; null → `NotFoundException`. Map to `EventDetailResponse`.
- **GetPublishedAsync:** call `GetPublishedAsync(page, pageSize)`, wrap in `PagedResponse`.
  Guard `page >= 1 && pageSize in [1..100]` → else `ValidationException`.
- **PublishAsync:** load event; null → `NotFound`; `OrganizerId != organizerId` →
  `ForbiddenException`; set `Status = Published`, save.

### TicketTypeService
- **AddAsync:** load event; null → `NotFound`; ownership check → `Forbidden`. Build
  `TicketType` with `AvailableQuantity = TotalQuantity`, persist, map.
- **GetByEventIdAsync:** verify event exists (→ `NotFound`), return mapped list.

### ReservationService — the core (README §4)

`CreateHoldAsync` implements the optimistic-concurrency flow:

```csharp
public async Task<ReservationResponse> CreateHoldAsync(
    Guid ticketTypeId, CreateReservationRequest request, Guid userId)
{
    if (request.Quantity < 1)
        throw new ValidationException("Quantity must be at least 1.");

    var ticketType = await _ticketTypes.GetByIdAsync(ticketTypeId)
        ?? throw new NotFoundException("Ticket type not found.");

    if (ticketType.AvailableQuantity < request.Quantity)
        throw new ConflictException("Insufficient inventory.");

    // Tracked mutation — RowVersion goes into the UPDATE ... WHERE automatically.
    ticketType.AvailableQuantity -= request.Quantity;

    var reservation = new Reservation
    {
        TicketTypeId     = ticketTypeId,
        UserId           = userId,
        Quantity         = request.Quantity,
        Status           = ReservationStatus.Pending,
        HoldExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
        CreatedAtUtc     = DateTime.UtcNow
    };
    await _reservations.AddAsync(reservation);

    try
    {
        await _reservations.SaveChangesAsync();   // single SaveChanges: decrement + insert
    }
    catch (DbUpdateConcurrencyException)
    {
        // Another request booked the same row first; its RowVersion no longer matches.
        throw new ConflictException("Someone booked first — please retry.");
    }

    return Map(reservation);
}
```

- **ConfirmAsync:** load (incl. `TicketType`); null → `NotFound`; not owner → `Forbidden`;
  not `Pending` or `HoldExpiresAtUtc < UtcNow` → `ConflictException("already expired/confirmed")`.
  Set `Confirmed` + `ConfirmedAtUtc`, save.
- **CancelAsync:** load; null → `NotFound`; not owner → `Forbidden`; restore
  `AvailableQuantity += Quantity`, set `Cancelled`, save.
- **GetMyReservationsAsync:** `GetByUserIdAsync`, map.
- **ExpireHoldsAsync:** `GetExpiredHoldsAsync()` (already includes `TicketType`); for each,
  set `Expired` and restore inventory; one `SaveChangesAsync`; return count.

**Teaching points:**
1. Steps 2–4 are one `DbContext` round trip. A read-then-write across two calls *without*
   the token is exactly the overbooking bug the `RowVersion` prevents.
2. The `catch` deliberately lives in the service (README §4 step 4), converting the EF-level
   exception into a domain `ConflictException` the API maps to `409`.
3. `GetByIdAsync` returns a **tracked** entity (repo uses `FindAsync`/`FirstOrDefaultAsync`,
   not `AsNoTracking`) — required for the mutation to be picked up by `SaveChanges`.

---

## Step 8 — Configuration (`appsettings.json`)

Add a `Jwt` section. **The signing key is a secret — do not commit a real one.** For local
dev use `dotnet user-secrets` or a gitignored `appsettings.Development.json`; in production
use environment variables / a secrets manager.

```json
"Jwt": {
  "Issuer": "Seatsure",
  "Audience": "SeatsureClients",
  "ExpiresInMinutes": 60,
  "Key": "" // set via user-secrets: dotnet user-secrets set "Jwt:Key" "<32+ char random>"
}
```

```bash
cd Seatsure
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)"
```

---

## Step 9 — Register services in DI (`Program.cs`)

Add after the repository registrations:

```csharp
using Seatsure.BLL.Security;
using Seatsure.BLL.Services.Interfaces;
using Seatsure.BLL.Services.Imp;

// Bind JwtSettings from configuration
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

// Security helpers
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();

// Business services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<ITicketTypeService, TicketTypeService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
```

`PasswordHasher` is stateless → `Singleton`. `TokenService` is `Scoped` for consistency
with the rest (it could be singleton, but scoped avoids surprises if it later takes scoped
deps). JWT Bearer *authentication* middleware is added in Phase 3.

---

## Step 10 — Build

```bash
dotnet build
```

Expected: 0 errors, 0 warnings.

---

## Verification

No controllers exist yet, so verify at the build + wiring level:

1. `dotnet build` succeeds with 0 warnings.
2. `dotnet run --project Seatsure` starts without DI resolution errors — this proves every
   service's constructor dependencies resolve from the container (repos + hasher + token
   service + options all wire up). Ctrl-C to stop.
3. Spot-check the concurrency path is a single `SaveChanges` by reading
   `ReservationService.CreateHoldAsync` — the decrement and the insert must be flushed by
   one call, with the `catch (DbUpdateConcurrencyException)` present.

End-to-end HTTP verification (register → create event → add ticket type → reserve →
confirm, plus the two-terminal `409` oversell demo) happens in Phase 3 once controllers and
JWT Bearer validation are wired.

---

## Out of scope (later phases)

| Deferred | Phase | Reason |
|---|---|---|
| Controllers + Problem Details mapping | 3 | Presentation layer maps exceptions → HTTP |
| JWT Bearer validation middleware, `[Authorize]` | 3 | API concern; BLL only *issues* tokens |
| Field-level validation (required, email, lengths) | 3 | Data annotations on DTOs + `[ApiController]` |
| SignalR `AvailabilityChanged` broadcasts | later | Hub doesn't exist yet |
| `HoldExpiryService` background host | later | Wraps `ExpireHoldsAsync` via `IServiceScopeFactory` |
| xUnit unit/integration tests | later | Proves the `409` + expiry logic |

