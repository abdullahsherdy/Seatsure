# Session 6 — Repository Pattern, DbContext & the Unit of Work, and "Does Business Logic Exist Here?"

> Teaching notes for the SeatSure internship. Every claim is anchored to real code in this repo
> (`file:line`). Conceptual points use the frame: **idea → principle → why → effect → tradeoffs**.
> Live coding stays in the existing layered projects (`DAL` / `BLL`); the Clean Architecture mapping
> is taught verbally, not by renaming.

---

## 0. Session at a glance

| Block | Point | Time | Format |
|---|---|---|---|
| A | Repository pattern — benefits, when NOT to | 20 min | whiteboard + code tour |
| B | DbContext vs repository (the Unit of Work reveal) | 20 min | code tour |
| C | "Does business logic exist here?" — the litmus test | 15 min | code tour |
| D | Clean Architecture mapping (verbal only) | 10 min | whiteboard |
| E | **Live coding**: `IUnitOfWork` refactor → wire `ReservationsController` | 45 min | live |
| F | Q&A | 15 min | discussion |

**The one load-bearing idea** (README line 93): *"one business rule, one service … the 'why does this
class exist' conversation."* Everything below serves that sentence. If interns leave able to answer
"why does `ReservationService` exist and why is the repo not allowed to do its job?", the session worked.

---

## 1. Pre-flight (read before class)

- **Doc vs reality:** `CLAUDE.md` says services/repos/DTOs/JWT are "not yet built." They *are* built.
  Only **controllers** and the **SignalR hub** are missing. Teach from the working code; fix CLAUDE.md later.
  
- **The fact that makes Block B work:** in [Program.cs:16-23](../Seatsure/Program.cs#L16-L23) the `AppDbContext`
  and all four repositories are registered **`Scoped`**. So within one HTTP request, every repository
  wraps the **same** `AppDbContext` instance. Hold this fact; it is the punchline of Block B.
- **Files to have open:** `ReservationService.cs`, `TicketTypeRepository.cs`, `ReservationRepository.cs`,
  `AppDbContext.cs`, `Program.cs`, `AppExceptions.cs`.

---

## 2. Block A — Repository pattern: benefits, and when NOT to

**Idea.** A repository is a thin, intention-revealing seam over data access. In this repo it is one
interface per entity — [ITicketTypeRepository.cs](../Seatsure.DAL/Repositories/Interfaces/ITicketTypeRepository.cs),
[IReservationRepository.cs](../Seatsure.DAL/Repositories/Interfaces/IReservationRepository.cs) — with a
matching implementation in `Repositories/Impl`. No generic `IRepository<T>`. (This is what
`exp.md` line 3 meant: keep `IUserRepository`, skip the generic base.)

**Principle.** *Depend on abstractions, not concretions* (DIP). The service layer depends on
`ITicketTypeRepository`, never on `AppDbContext` or `DbSet<TicketType>` directly.

**Why it's here — the honest, defensible reason.** Look at
[ReservationService.cs:19-31](../Seatsure.BLL/Services/ReservationService.cs#L19-L31): the service takes
`IReservationRepository`, `ITicketTypeRepository`, `IAvailabilityNotifier`. **Not** a `DbContext`.
That means the concurrency logic can be unit-tested with three fakes and zero database. README lines
187-188 demand exactly this: unit tests on `IReservationService` for the 409-on-oversell case. The
repository seam is what makes that test possible without SQL Server.

**Effect.** Three concrete payoffs, each visible in the code:
1. **Testability** — mock `ITicketTypeRepository.GetByIdAsync` to return a `TicketType` with
   `AvailableQuantity = 1`, then assert two concurrent holds produce one `ConflictException`.
2. **Query centralization** — the `.Include(r => r.TicketType)` lives in one place
   ([ReservationRepository.cs:13-16](../Seatsure.DAL/Repositories/Imp/ReservationRepository.cs#L13-L16)),
   not scattered across services.
3. **Vocabulary** — `GetExpiredHoldsAsync()` names a domain concept; `_context.Reservations.Where(r =>
   r.Status == Pending && r.HoldExpiresAtUtc < now)` names an implementation.

**Tradeoffs — when NOT to add it.** Say this out loud so interns don't cargo-cult the pattern:
- `DbSet<T>` **is already** a repository (`Add`, `Find`, `Remove`, LINQ queries).
- `DbContext` **is already** a Unit of Work (change tracking + one `SaveChanges` = one transaction).
- So a repository that only forwards to `DbSet` (see
  [TicketTypeRepository.cs:13-27](../Seatsure.DAL/Repositories/Imp/TicketTypeRepository.cs#L13-L27) —
  every method is a one-liner over `_context`) buys you the seam **and nothing else**. That's fine
  *if* you value the seam (we do, for tests). It is pure ceremony if you don't test and never intend to
  swap the provider.
- Rule of thumb to give them: **"Add a repository when you have a reason to fake or rename the data
  access. Otherwise `DbContext` is enough."**

---

## 3. Block B — DbContext vs repository: the Unit of Work reveal

This is the block that changes how they see the whole codebase. Do it as a live trace.

**Idea.** The repositories look independent, but they are not — they are windows onto **one shared
change-tracker**. The transaction boundary is the `DbContext`, not any single repository.

**The trace — walk `CreateHoldAsync` line by line**
([ReservationService.cs:33-73](../Seatsure.BLL/Services/ReservationService.cs#L33-L73)):

1. Line 39 — `_ticketTypes.GetByIdAsync(id)` → `FindAsync`
   ([TicketTypeRepository.cs:13-14](../Seatsure.DAL/Repositories/Imp/TicketTypeRepository.cs#L13-L14)).
   This returns a **tracked** entity. Its `RowVersion` is now known to the context.
2. Line 47 — `ticketType.AvailableQuantity -= request.Quantity;` mutates that tracked entity.
   No repository call. The change tracker already knows.
3. Line 58 — `_reservations.AddAsync(reservation)` stages an **insert** — on a **different repository**.
4. Line 64 — `_reservations.SaveChangesAsync()`.

**Ask the class: "How many rows does line 64 write?"** The instinctive answer is "one — the
reservation." The correct answer is **two**: the `UPDATE TicketTypes …` (the decrement) *and* the
`INSERT Reservations …`, in one transaction.

**Why.** Because both repositories share the same scoped `AppDbContext`
([Program.cs:16-23](../Seatsure/Program.cs#L16-L23)). Calling `SaveChanges` on *either* repository
flushes **every** tracked change on that context. `_reservations.SaveChangesAsync()` commits the ticket
decrement too. **That is the Unit of Work.** (This is `exp.md` line 5 made concrete: "DbContext is a
unit of work, so no need for a separate unit of work interface.")

**Effect / the smell this exposes.** Notice the choice at line 64 is *arbitrary*. We could have written
`_ticketTypes.SaveChangesAsync()` and gotten the identical result. When "which repository do I call
`SaveChanges` on?" has no principled answer, the method is on the wrong object. `SaveChanges` is a
**transaction** operation, not a **ticket** or **reservation** operation. That mismatch is the setup
for the Block E refactor.

**Tradeoffs.** Two defensible designs; make them pick with eyes open:
- **(a) Keep `DbContext` as the UoW, drop per-repo `SaveChanges`** — inject the context (or a one-method
  `IUnitOfWork`) and commit there. Honest boundary.
- **(b) Per-repo `SaveChanges`** (current code) — convenient, but lies about the boundary and invites
  "save after every repo call," which fragments one transaction into many.

---

## 4. Block C — "Does business logic exist here?" (the litmus test)

**Idea.** Business logic = **decisions and rules**. Data access = **fetch and persist**. A class earns
its existence by owning decisions.

**The litmus test to write on the board:**
> "Is this line making a *decision* (a rule, a policy, a state transition), or just *moving data* in or
> out of the store? Decisions live in the service. Movement lives in the repository."

**Apply it to the real code — the repository has ZERO decisions:**
Every method in [TicketTypeRepository.cs](../Seatsure.DAL/Repositories/Imp/TicketTypeRepository.cs) and
[ReservationRepository.cs](../Seatsure.DAL/Repositories/Imp/ReservationRepository.cs) is fetch/persist.
No `if`, no rule, no throw. That is correct. A repository with an inventory check in it would be a bug.

**Apply it to the real code — the service is ALL decisions**
([ReservationService.cs](../Seatsure.BLL/Services/ReservationService.cs)):

| Line | The decision (business rule) |
|---|---|
| 35-36 | `Quantity < 1` → `ValidationException` (400) |
| 43-44 | requested > available → `ConflictException` (409, insufficient inventory) |
| 47 | the decrement itself — the core state change |
| 55 | hold expiry policy = now + 10 min (`HoldMinutes`) |
| 62-69 | concurrency conflict → **409** ("someone booked first") |
| 80-81 | ownership check → `ForbiddenException` (403) |
| 83-88 | legal state transitions (can't confirm a non-pending or expired hold) |
| 108-112 | cancel restores inventory — but only from active states (no double-restore) |

**Why this split matters.** Point at [ReservationService.cs:15](../Seatsure.BLL/Services/ReservationService.cs#L15):
`internal sealed class ReservationService`. The concrete class is **invisible** outside the BLL assembly;
callers only ever see `IReservationService`. The rules have exactly one home, and that home is sealed
shut. This is the physical embodiment of "one business rule, one service."

**Effect.** When a rule changes (say holds become 5 minutes), there is exactly one line to change
(line 17, `HoldMinutes`) and exactly one class to re-test. That is the payoff of concentrating decisions.

**Tradeoffs.** The danger is the **anemic service** (a service that just forwards to the repo, adding no
decision) and its twin, the **fat repository** (a repo that sneaks in rules). Give them the tell: *if a
service method has no `if`/`throw`/state change and no orchestration of ≥2 repos, ask whether it should
exist at all.* `GetByUserAsync` ([ReservationService.cs:119-123](../Seatsure.BLL/Services/ReservationService.cs#L119-L123))
is borderline — it only maps to DTO. That's acceptable (mapping + a stable interface for the controller),
but it's a good one to debate with the class.

---

## 5. Block D — Clean Architecture mapping (verbal, no rename)

We are **not** renaming projects. We are teaching the mental map so interns can read a Clean Architecture
codebase later.

```
This repo (layered)          Clean Architecture           What lives there
--------------------         -------------------          --------------------------------
Seatsure.Domain        ≈     Domain / Entities            entities, enums (no dependencies)
Seatsure.BLL           ≈     Application                  use-cases, service interfaces, DTOs
Seatsure.DAL           ≈     Infrastructure               EF Core, repositories, DbContext
Seatsure (API)         ≈     Presentation / API           controllers, DI composition root
```

**The one real difference to name (not just a rename):** in strict Clean Architecture the **interfaces**
live in Application and the **implementations** in Infrastructure, so the dependency arrow points
*inward* (Infrastructure → Application). In our repo the repository interfaces live in the DAL
([Repositories/Interfaces](../Seatsure.DAL/Repositories/Interfaces/)) **next to** their implementations,
and the BLL depends on the DAL. That is the pragmatic layered choice. Say clearly: *both are valid; Clean
Architecture buys stricter dependency inversion at the cost of more indirection, and we chose the simpler
layering for a teaching codebase.* Do not let them think "Clean Architecture = correct, layered = wrong."

---

## 6. Block E — Live coding (answer key)

Two segments. **Keep the build green at every step** — that's why we go *additive first*, then clean up.

### Segment E1 — Introduce `IUnitOfWork` (dramatizes Blocks A & B) — ~20 min

**Goal:** make the transaction boundary honest. The service should say "commit this unit of work," not
"save via the reservations repo."

**Step 1 — new files in the DAL.**

```csharp
// Seatsure.DAL/IUnitOfWork.cs
namespace Seatsure.DAL;

/// <summary>
/// The transactional boundary. One SaveChanges commits every tracked change across every
/// repository sharing this scope's AppDbContext. (DbContext already IS a unit of work; this
/// interface just gives that fact an honest, testable name the BLL can depend on.)
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

```csharp
// Seatsure.DAL/UnitOfWork.cs
namespace Seatsure.DAL;

internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    public UnitOfWork(AppDbContext context) => _context = context;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
```

**Step 2 — migrate `ReservationService` to it.** Inject `IUnitOfWork`
([constructor at ReservationService.cs:23-31](../Seatsure.BLL/Services/ReservationService.cs#L23-L31)),
then replace the four `_reservations.SaveChangesAsync()` / `_x.SaveChangesAsync()` calls (lines 64, 92,
113, 137) with `_unitOfWork.SaveChangesAsync()`. The concurrency `try/catch` at
[lines 62-69](../Seatsure.BLL/Services/ReservationService.cs#L62-L69) is unchanged — the
`DbUpdateConcurrencyException` now surfaces from the UoW, and still maps to `ConflictException` (409).

**Step 3 — register it.** `UnitOfWork` is `internal`, so it can't be registered from the API project.
This is the nudge to give the DAL its own DI extension (mirroring `AddBll` in
[DependencyInjection.cs](../Seatsure.BLL/DependencyInjection.cs)):

```csharp
// Seatsure.DAL/DependencyInjection.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Seatsure.DAL.Repositories.Impl;
using Seatsure.DAL.Repositories.Interfaces;

namespace Seatsure.DAL;

public static class DependencyInjection
{
    public static IServiceCollection AddDal(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(o =>
            o.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<ITicketTypeRepository, TicketTypeRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }
}
```

Then [Program.cs:16-26](../Seatsure/Program.cs#L16-L26) collapses to:

```csharp
builder.Services.AddDal(builder.Configuration);
builder.Services.AddBll(builder.Configuration);
```

> **One-line alternative if you want to skip the DI extension live:** make `UnitOfWork` `public` and
> add `builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();` next to the repos. Less clean, but zero
> new files. Recommend the `AddDal` path — it parallels `AddBll` and cleans up `Program.cs`.

**Step 4 — the honest discussion (do NOT skip).** Now that `IUnitOfWork` exists, ask: *"Did we just add
value, or ceremony?"* Answer both sides:
- **Value:** the boundary is named and honest; the BLL no longer pretends "saving" belongs to a repo; the
  service is still fully fakeable in tests (mock `IUnitOfWork.SaveChangesAsync`).
- **Ceremony:** `UnitOfWork` is a one-line forward to `DbContext.SaveChangesAsync`, which was *already* a
  unit of work. In a small app, injecting `AppDbContext` directly into the service would be defensible too.
- **Verdict to give them:** we added it because it (1) reads honestly and (2) keeps the BLL free of a
  direct `DbContext` dependency. Both are judgment calls, not laws.

**Homework / clean finish (mention, don't do live):** remove `SaveChangesAsync()` from the repository
*interfaces* and impls. This is a **breaking interface change** — the other three services
(`AuthService`, `EventService`, `TicketTypeService`) also call `_repo.SaveChangesAsync()` and must move
to `IUnitOfWork` in the same commit or the build breaks. That blast radius is itself the lesson:
**interface changes ripple to every caller** (Interface Segregation earns its keep here).

### Segment E2 — Wire `ReservationsController` (shows the full layered flow) — ~25 min

**Goal:** one HTTP request travels API → BLL → DAL → DB and back, and a concurrency conflict becomes a
real HTTP **409**. This is the demo README line 131 demands ("two tabs firing at once").

**Step 1 — the exception-handling middleware.** This is the *missing consumer* of the design already in
[AppExceptions.cs:5-9](../Seatsure.BLL/Exceptions/AppExceptions.cs#L5-L9): the abstract `StatusCode` exists
precisely so middleware can map any `AppException` to a status. Nothing consumes it yet — we write that now.

```csharp
// Seatsure/Middleware/ExceptionHandlingMiddleware.cs
using Seatsure.BLL.Exceptions;

namespace Seatsure.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)                       // expected business failure
        {
            await Results.Problem(title: ex.Message, statusCode: ex.StatusCode)
                         .ExecuteAsync(context);
        }
        catch (Exception ex)                          // unexpected: log detail, leak nothing
        {
            _logger.LogError(ex, "Unhandled exception on {Path}", context.Request.Path);
            await Results.Problem(title: "An unexpected error occurred.", statusCode: 500)
                         .ExecuteAsync(context);
        }
    }
}
```

Register it **first** in the pipeline in [Program.cs](../Seatsure/Program.cs) (before routing):
`app.UseMiddleware<ExceptionHandlingMiddleware>();`

> **Teaching beats:** (1) the `AppException` catch maps `ConflictException.StatusCode == 409`
> ([AppExceptions.cs:43-46](../Seatsure.BLL/Exceptions/AppExceptions.cs#L43-L46)) with no `switch` — polymorphism
> does the mapping. (2) the generic catch **logs server-side but returns a generic 500** — this is the
> security rule "error messages don't expose internal details" in action.

**Step 2 — JWT bearer validation.** Token *generation* exists
([JwtTokenService.cs](../Seatsure.BLL/Security/JwtTokenService.cs)) but *validation* is not wired —
[Program.cs:44](../Seatsure/Program.cs#L44) calls `UseAuthorization()` with no `AddAuthentication()` and
no `UseAuthentication()`. Reservation actions are authenticated (README §3.4), so add:

```csharp
// top of Program.cs
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear(); // keep "sub"/"role" literal, no URI remap

var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,           ValidIssuer   = jwt["Issuer"],
        ValidateAudience = true,         ValidAudience = jwt["Audience"],
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)),
        NameClaimType = "sub",           // matches JwtTokenService.cs:24-26
        RoleClaimType = "role",
    });
```

Then **fix the middleware order** — this is the classic gotcha:
```csharp
app.UseAuthentication();   // NEW — must come BEFORE UseAuthorization
app.UseAuthorization();
```

> **If you're time-boxed and want E2 to be purely about the layered flow**, skip Step 2, put
> `[AllowAnonymous]` on the controller, and read a hard-coded `userId` for the demo. Say explicitly that
> full JWT bearer wiring is the next session. (Recommended only if E1 ran long.)

**Step 3 — the controller.** Routes are absolute (`/api/...`) because the four endpoints live under three
different prefixes (README §3.4) — a controller-level `[Route]` prefix wouldn't fit all of them.

```csharp
// Seatsure/Controllers/ReservationsController.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seatsure.BLL.DTOs.Reservations;
using Seatsure.BLL.Services.Interfaces;

namespace Seatsure.Controllers;

[ApiController]
[Authorize]
public sealed class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservations;
    public ReservationsController(IReservationService reservations) => _reservations = reservations;

    // "sub" claim carries User.Id (JwtTokenService.cs:24). Requires NameClaimType="sub" + cleared map.
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue("sub") ?? throw new UnauthorizedAccessException("Missing sub claim."));

    // §3.4  POST /api/ticket-types/{id}/reservations  { quantity }  -> 201 | 400 | 404 | 409
    [HttpPost("/api/ticket-types/{id:guid}/reservations")]
    public async Task<ActionResult<ReservationDto>> CreateHold(Guid id, CreateReservationRequest request)
    {
        var dto = await _reservations.CreateHoldAsync(id, CurrentUserId, request);
        return Created($"/api/users/me/reservations", dto); // contract has no GET-single; point at the collection
    }

    // §3.4  POST /api/reservations/{id}/confirm  -> 200 | 404 | 409
    [HttpPost("/api/reservations/{id:guid}/confirm")]
    public async Task<ActionResult<ReservationDto>> Confirm(Guid id) =>
        Ok(await _reservations.ConfirmAsync(id, CurrentUserId));

    // §3.4  POST /api/reservations/{id}/cancel  -> 200 | 403 | 404
    [HttpPost("/api/reservations/{id:guid}/cancel")]
    public async Task<ActionResult<ReservationDto>> Cancel(Guid id) =>
        Ok(await _reservations.CancelAsync(id, CurrentUserId));

    // §3.4  GET /api/users/me/reservations  -> 200 | 401
    [HttpGet("/api/users/me/reservations")]
    public async Task<ActionResult<IEnumerable<ReservationDto>>> GetMine() =>
        Ok(await _reservations.GetByUserAsync(CurrentUserId));
}
```

**Step 4 — the trace to narrate.** One request, all layers:
`POST /api/ticket-types/{id}/reservations` → `ReservationsController.CreateHold` → `IReservationService.CreateHoldAsync`
→ `ITicketTypeRepository` + `IReservationRepository` (shared `AppDbContext`) → `IUnitOfWork.SaveChangesAsync`
→ SQL Server. On conflict, `DbUpdateConcurrencyException` → `ConflictException` (409) → middleware →
Problem Details. **Point out the controller has zero business logic** — it extracts the user id, calls one
service method, and shapes the HTTP response. That's the whole job of a controller. (Reinforces Block C.)

**Step 5 — the money demo (README line 131).** Set a ticket type's `AvailableQuantity = 1`. Fire two
`CreateHold` requests concurrently (two Swagger tabs / two `.http` sends / two `curl`). One returns **201**,
one returns **409 "Someone booked first. Please retry."** Inventory never goes negative. *This is the whole
project in ten seconds.*

---

## 7. Block F — Q&A bank (anticipated questions + crisp answers)

- **"If `DbContext` is already a Unit of Work, why did we build `IUnitOfWork`?"**
  To give the BLL an honest, fakeable name for "commit" without depending on EF Core directly. It's a
  judgment call, not a requirement — in a tiny app, injecting `AppDbContext` is fine.

- **"Why per-entity interfaces instead of a generic `IRepository<T>`?"**
  A generic base pushes every entity to share one API, then you bolt on `GetExpiredHoldsAsync` anyway.
  Per-entity interfaces say exactly what each aggregate needs and nothing more (Interface Segregation).

- **"Could the repository do the inventory check?"**
  No — that's a decision, and decisions belong in the service (Block C litmus test). A repo that throws
  `ConflictException` has become a service wearing a repo's name.

- **"Why does `CreateHold` call `SaveChanges` on the reservations repo when it also changed the ticket?"**
  Trick question — pre-refactor, that only *worked* because both repos share the context. Post-refactor
  it calls `IUnitOfWork`, which is the honest answer. (This is the Block B → E arc.)

- **"How does a `DbUpdateConcurrencyException` become a 409?"**
  `RowVersion` is the concurrency token ([AppDbContext.cs:51](../Seatsure.DAL/AppDbContext.cs#L51)); EF puts
  the original value in the `UPDATE … WHERE RowVersion = @orig`. If a competitor changed the row, 0 rows
  match, EF throws, the service catches and throws `ConflictException` (409), middleware renders it.

- **"Why is `ReservationService` `internal sealed`?"**
  `internal` = only the BLL sees the concrete type; callers get `IReservationService`. `sealed` = no
  subclass can weaken the invariants. One rule, one home, locked.

- **"Isn't wrapping EF in repositories just fighting the framework?"**
  Sometimes yes — say so. We keep it for the test seam (README lines 187-188). If we never tested and never
  swapped providers, injecting `DbContext` directly would be the leaner call.

---

## 8. Instructor cheat-sheet (file:line index)

| Concept | Where |
|---|---|
| Concurrency token declared | [AppDbContext.cs:51](../Seatsure.DAL/AppDbContext.cs#L51), [TicketType.cs:12](../Seatsure.Domain/TicketType.cs#L12) |
| The concurrency flow | [ReservationService.cs:33-73](../Seatsure.BLL/Services/ReservationService.cs#L33-L73) |
| 409 catch | [ReservationService.cs:62-69](../Seatsure.BLL/Services/ReservationService.cs#L62-L69) |
| Repo = zero decisions | [TicketTypeRepository.cs:13-27](../Seatsure.DAL/Repositories/Imp/TicketTypeRepository.cs#L13-L27) |
| Shared scoped context | [Program.cs:16-23](../Seatsure/Program.cs#L16-L23) |
| Exception → status seam | [AppExceptions.cs:5-9](../Seatsure.BLL/Exceptions/AppExceptions.cs#L5-L9) |
| JWT claims (sub/role) | [JwtTokenService.cs:22-28](../Seatsure.BLL/Security/JwtTokenService.cs#L22-L28) |
| BLL DI pattern to mirror | [DependencyInjection.cs](../Seatsure.BLL/DependencyInjection.cs) |
| Frozen route contract | README §3.4 |

## 9. Pre-class checklist

- [ ] `dotnet build` is green on `main`.
- [ ] Decide E2 scope: full JWT bearer, or `[AllowAnonymous]` + hard-coded user id if E1 ran long.
- [ ] Seed one event + one ticket type with `AvailableQuantity = 1` for the two-tab demo.
- [ ] Two Swagger tabs (or a `.http` file with two sends) ready for the concurrency demo.
- [ ] Fix `CLAUDE.md` implementation-status section after class (services/repos/DTOs/JWT are built).
