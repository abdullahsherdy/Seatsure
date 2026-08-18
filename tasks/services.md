# Task — Application Service Layer (DTOs, Exceptions, Services)

> **Goal:** Implement the business-logic layer of SeatSure inside `clean-arch/Seatsure.Application`. You will build the **DTOs**, the **exception types**, the **supporting ports** the services need, and the **services** themselves (interface + implementation for each). This is the layer that turns the repositories you already wrote into real, rule-enforcing behavior.

> **Scope discipline:** Only touch the `clean-arch/` solution. Ignore `N-tier arch/` entirely — it is legacy and not part of the build.

---

## 0. Where we are

**Already done (do not rebuild):**
- Domain entities (`User`, `Event`, `TicketType`, `Reservation`) and enums (`UserRole`, `EventStatus`, `ReservationStatus`) — `Guid` primary keys, `TicketType.RowVersion` is a `byte[]` concurrency token.
- `AppDbContext` with fluent config + migrations.
- Repository **interfaces** in `Seatsure.Application.Interfaces` and their EF **implementations** in `Seatsure.Infrastructure.Repositories`.
- DI wiring for the `DbContext` and the four repositories in `Program.cs`.

**A gap you must notice before starting:** the repositories only expose `GetBy…`, `AddAsync`, and query methods. `AddAsync` **only stages** an entity in the shared scoped `DbContext` — **nothing calls `SaveChanges`**. There is no `IUnitOfWork`. Part 3 of this task is where you resolve that; every write-service depends on it.

**Out of scope for this task (later phases — do not build now):** controllers, JWT Bearer authentication middleware, Problem Details middleware, SignalR hub, the hold-expiry `BackgroundService`, and the test project. You are building the layer those pieces will sit on top of.

---

## 1. Learning objectives

By the end you should be able to explain, out loud, each of these:

1. **Why a service layer exists at all** — what belongs in a service that does *not* belong in a controller or a repository.
2. **Why entities never cross the service boundary** — the role of DTOs as a contract.
3. **Why business failures are modelled as typed exceptions** instead of return codes or booleans, and the tradeoffs of that choice.
4. **Where the "commit" belongs** in clean architecture, and why one shared scoped `DbContext` is already a unit of work.
5. **The concurrency flow** (Part 6) — how `RowVersion` prevents overselling, and the exact path from `DbUpdateConcurrencyException` to an HTTP `409`. This is the load-bearing lesson of the whole project.

---

## 2. Layering rules (read before writing a single file)

Everything you build in this task lives in **`Seatsure.Application`**, which may reference **`Seatsure.Domain` only**.

**Hard rules — a review will reject violations:**
- **No EF Core in Application.** No `DbContext`, no `Microsoft.EntityFrameworkCore` using-statements, no LINQ-to-entities. The Application layer talks to persistence only through the repository interfaces (ports).
- **No ASP.NET / HTTP types in Application.** No `HttpContext`, no `IActionResult`, no status-code constants. Services throw exceptions; the controllers (later) decide the HTTP mapping.
- **No entities leaving the service.** Service method parameters and return values are DTOs, never `User` / `Event` / `TicketType` / `Reservation`.
- **Dependencies point inward.** Application depends on Domain. Infrastructure depends on Application. Never the reverse.

Suggested new folders inside `Seatsure.Application`:
- `DTOs/` (optionally split into subfolders per feature area)
- `Exceptions/`
- `Services/` (interfaces + implementations, or split `Services/Interfaces` and `Services/Implementations` — pick one and stay consistent)
- `Interfaces/` already exists (repository ports) — the new supporting ports from Part 4 go here too.

---

## 3. Deliverables checklist

- [ ] All DTOs (Part 4)
- [ ] All exception types (Part 5)
- [ ] Commit mechanism decision + implementation (Part 6)
- [ ] Supporting ports: password hasher + token generator (Part 7)
- [ ] `IAuthService` + implementation (Part 8)
- [ ] `IEventService` + implementation (Part 8)
- [ ] `ITicketTypeService` + implementation (Part 8)
- [ ] `IReservationService` + implementation (Part 8) — the core
- [ ] DI registration for every new service and port (Part 9)
- [ ] Solution builds green (Part 10)

---

## 4. DTOs

**Principle:** a DTO is the shape of data crossing a boundary. Entities are the shape of data *inside* the domain. Keeping them separate means you can change the database model without breaking your API, and you never accidentally leak a `PasswordHash` or an internal navigation property to a caller.

**Rules for every DTO:**
- Declare them as `record` types (immutable, value-based — the right tool for a data contract).
- Property names in `camelCase` when serialized; UTC timestamps carry a `Utc` suffix in their name.
- **Never** reference a Domain entity or enum-heavy entity graph from a DTO. If a DTO needs a status, expose it as a simple string or the enum, but never embed the whole entity.
- Separate **input** DTOs (what a caller sends) from **output** DTOs (what a caller receives). Do not reuse one record for both — they evolve differently and have different validation needs.

**Steps:**
1. Create a **generic paged-result DTO** to carry list responses. It must hold the items plus `page`, `pageSize`, and `totalCount` (this matches the pagination envelope in the frozen spec). Every list endpoint returns this shape.
2. Create the **Auth** DTOs: a registration input (name, email, password, role), a login input (email, password), and an auth-result output (the token string and its `expiresAtUtc`). The result must **not** carry the password hash or any entity.
3. Create the **Event** DTOs: a create-event input (title, description, venue name, start time), and an event output that exposes only what a client should see (id, title, description, venue, start time, status, organizer id). Decide deliberately whether the output includes its ticket types or not, and be able to justify it.
4. Create the **TicketType** DTOs: an add-ticket-type input (name, price, total quantity), and a ticket-type output (id, event id, name, price, total quantity, **available quantity**). Note that `RowVersion` is an internal concurrency concern and must **not** appear in any DTO.
5. Create the **Reservation** DTOs: a create-hold input (quantity), and a reservation output (id, ticket type id, quantity, status, `holdExpiresAtUtc`, `createdAtUtc`, and `confirmedAtUtc` when set).

**Think about:** which fields are server-controlled and must never be accepted from the client (ids, timestamps, status, available quantity, the organizer id on an event). If an input DTO contains one of those, that is a bug — the client does not get to set it.

---

## 5. Exception types

**Principle:** the service layer signals *what went wrong in business terms* — "that event doesn't exist", "that email is taken", "someone booked the last ticket first". It must not know or care that those become HTTP 404 / 409 / etc. — that translation is the controller's job in a later task. Typed exceptions are how the service communicates the *category* of failure without depending on the web layer.

**Tradeoff to understand and be ready to defend:** exceptions vs. a result/outcome object (e.g., a discriminated result). Exceptions keep the happy-path code clean and are idiomatic in ASP.NET, but they use control flow for expected conditions and cost more when thrown frequently. For this project we use exceptions — know *why* you'd reconsider that if a failure became a common, expected outcome rather than an exceptional one.

**Steps — create one exception type per failure category.** Each should carry a clear message and, where useful, the identifier that was not found or that conflicted:
1. A **not-found** exception — an entity was requested by id and does not exist. (→ maps to 404 later.)
2. A **validation** exception — input broke a business rule the DTO shape alone can't express (e.g., quantity < 1, price ≤ 0, page < 1). (→ 400.)
3. A **conflict** exception — the request collided with the current state. This one is used in **two distinct situations**, so make sure its message can express both: (a) insufficient inventory / a concurrency conflict on a reservation hold, and (b) an illegal state transition such as confirming an already-expired or already-confirmed reservation. (→ 409.)
4. An **email-taken / duplicate** exception for registration when the email already exists. You may model this as its own type or as a specific conflict — decide and justify. (→ 409.)
5. An **authorization/ownership** exception — the caller is authenticated but not allowed to act on this resource (e.g., publishing an event they don't own). (→ 403.)

**Guidance:** give the exceptions a small, shared base if it helps you group them, but don't over-engineer a deep hierarchy. The goal is that a later middleware can pattern-match the exception type to a status code in one place.

---

## 6. The commit mechanism — an open design decision you must resolve

**The problem, precisely:** `AddAsync` on every repository calls `_context.Xs.AddAsync(entity)`, which only *stages* the entity in the change tracker. Until something calls `SaveChangesAsync`, nothing is written and no id is truly persisted. Your write-services (register, create event, add ticket type, create/confirm/cancel reservation) all need a way to **commit**.

**Key fact that shapes the answer:** all four repositories are registered as `Scoped` and share the **same** scoped `AppDbContext` per request. That means EF Core already gives you a unit of work — one `SaveChangesAsync` commits every change staged across every repository in that request, atomically.

**Your task:** choose one commit strategy, implement it, and write a two-or-three sentence note in the task (or a short ADR in `./artifacts/`) explaining the choice and its tradeoff. The realistic options:

| Option | What it looks like | Tradeoff |
|---|---|---|
| **`IUnitOfWork` port** (recommended) | A new interface in `Application.Interfaces` with a single `SaveChangesAsync`; implemented in Infrastructure over the shared `DbContext`; injected into services. | One honest name for "commit", one place to implement, keeps the layer boundary clean. Slightly more ceremony. |
| **`SaveChangesAsync` on each repository** | Add a commit method to the repo interfaces. | Fewer new types, but ambiguous — which repo "owns" the commit when they share a context? Invites calling save in the wrong place. |
| **Inject the `DbContext` into services** | Services call `context.SaveChangesAsync` directly. | Simplest to write, but **breaks the layering rule** (EF in Application). Only acceptable as a discussed, documented shortcut — not recommended here. |

**Recommendation:** implement the `IUnitOfWork` port. It names the concept correctly, keeps EF out of Application, and makes the "one request = one transaction" story explicit. Whatever you pick, the concurrency flow in Part 6 depends on the commit happening **inside a single service method** on the shared context.

---

## 7. Supporting ports for authentication

The auth service needs to hash passwords and issue tokens, but **neither concern belongs in the Application layer's implementation** — hashing algorithms and JWT signing are infrastructure details. So you define them as **ports (interfaces) in Application** and implement them in Infrastructure.

**Steps:**
1. Define a **password-hasher port** in `Application.Interfaces` with two responsibilities: produce a hash from a plaintext password, and verify a plaintext password against a stored hash. The service depends only on this interface.
2. Define a **token-generator port** in `Application.Interfaces` that takes the identity information a token needs (user id, email, role) and returns a signed token string plus its expiry. The service depends only on this interface.
3. Implement both in `Seatsure.Infrastructure` (a new folder such as `Security/` or `Auth/`). The password hasher can wrap ASP.NET Core Identity's `PasswordHasher` or a `BCrypt.Net` package — **pick one and pin it**. The token generator reads issuer/audience/key/expiry from the existing `Jwt` config section in `appsettings.json` and produces the JWT with claims `sub` (user id), `role`, and `email` per the spec.

**Why this split matters:** it keeps the auth *business logic* (does this user exist? is the password right? what claims go in the token?) testable without a real signing key or a real database, and it means swapping BCrypt for Identity later touches one Infrastructure class, not the service.

> Note: actually validating incoming JWTs (the authentication middleware and `[Authorize]`) is a **later** task. Here you only need to *issue* tokens so `login` can return one.

---

## 8. Services

For **each** service below: create the interface in the Application layer, then its implementation. Constructor-inject the repository ports, the unit-of-work (or chosen commit mechanism), and any supporting ports it needs. Methods take DTOs in and return DTOs out. Enforce every rule by throwing the appropriate exception from Part 5.

### 8.1 `IAuthService`
- **Register:** validate the input (email format/uniqueness intent, password present, valid role). Reject a duplicate email with the email-taken exception. Hash the password via the port. Create the `User` (set `CreatedAtUtc` in UTC), stage it via the user repository, commit. Return the created user's public data (never the hash).
- **Login:** look up the user by email; if missing or the password fails verification, throw the same failure for both cases (do not reveal which was wrong — an auth best practice). On success, issue a token via the token-generator port and return the token + `expiresAtUtc`.

### 8.2 `IEventService`
- **List published:** validate paging (`page ≥ 1`, `pageSize` within a sane bound); call the repository's published-events query; map to the paged-result DTO.
- **Get by id:** fetch; if missing, throw not-found; map to output DTO.
- **Create event (organizer only):** the caller's user id and role come in as a parameter (the controller will supply them from the token later — the service takes them as arguments, it does not read `HttpContext`). Validate the input; create the `Event` with `Status = Draft`, `OrganizerId = caller`, `CreatedAtUtc` in UTC; stage and commit; return the created event.
- **Publish event (organizer + owner only):** fetch the event (not-found if missing); verify the caller **owns** it (ownership exception if not — this is an authorization check, distinct from "is an organizer"); transition `Status` to `Published`; commit. Be able to explain why ownership is checked here in code and cannot be expressed by a role attribute alone.

### 8.3 `ITicketTypeService`
- **List for event:** ensure the event exists (not-found otherwise); return its ticket types as output DTOs.
- **Add ticket type (organizer + owner only):** fetch the event; verify ownership; validate the input (`price > 0`, `totalQuantity ≥ 1`); create the `TicketType` with `AvailableQuantity = TotalQuantity` initially; stage and commit; return it. Do not expose `RowVersion`.

### 8.4 `IReservationService` — the core of the project
This service holds all reservation logic. Read Part 6 (concurrency) before implementing `CreateHold`.

- **Create hold:** validate `quantity ≥ 1`. Fetch the **tracked** ticket type (the repository uses `FindAsync` — do **not** switch to `AsNoTracking`). If `quantity > AvailableQuantity`, throw the conflict exception (insufficient inventory). Otherwise decrement `AvailableQuantity` by `quantity`, create a `Reservation` with `Status = Pending`, `HoldExpiresAtUtc = UtcNow + 10 minutes`, `CreatedAtUtc = UtcNow`, `UserId = caller`. Stage the reservation and commit **in one `SaveChanges`**. Handle the concurrency exception per Part 6 → conflict. Return the reservation DTO.
- **Confirm:** fetch the reservation (not-found if missing); if it is not `Pending` (already expired/confirmed/cancelled), throw the conflict exception (illegal transition); set `Status = Confirmed`, `ConfirmedAtUtc = UtcNow`; commit; return it.
- **Cancel:** fetch the reservation (not-found); verify the caller **owns** it (ownership exception otherwise); if it is `Pending` or `Confirmed`, set `Status = Cancelled` and **restore** `AvailableQuantity += Quantity` on the ticket type; commit; return it. (Restoring inventory on cancel mirrors what the future hold-expiry background service will do on expiry.)
- **My reservations:** take the caller's user id; return their reservations as output DTOs.

---

## 9. Concurrency flow (the load-bearing lesson — implement `CreateHold` exactly this way)

This is *why* the project exists. Two people race for the last ticket; exactly one wins; inventory is never oversold.

**The mechanism, step by step:**
1. Read the `TicketType` **tracked** (via the repository's `FindAsync`/`FirstOrDefaultAsync`). This loads the current `RowVersion` into the change tracker.
2. Check `quantity <= AvailableQuantity` in memory. If it fails, that's a plain insufficient-inventory conflict — throw before touching anything.
3. Decrement `AvailableQuantity`. Because `RowVersion` is mapped with `IsRowVersion()`, EF Core will automatically put `WHERE Id = … AND RowVersion = <original>` into the generated `UPDATE`.
4. Commit with a **single** `SaveChangesAsync` inside this one service method — the read and the write share the same tracked context, so the token check is real.
5. If another request committed first, the row's `RowVersion` has moved on, the `WHERE` matches zero rows, and EF throws `DbUpdateConcurrencyException`. **Catch it in the service** and translate it into your conflict exception (which the controller will later render as HTTP `409` with a clear "someone booked first, please retry" message).

**Do NOT:**
- Do **not** read the ticket type with `AsNoTracking` — you lose the tracked original `RowVersion` and the whole mechanism silently stops working.
- Do **not** split the read and write across two separate requests/contexts without carrying the token — the lesson is lost and overselling becomes possible.
- Do **not** "fix" the concurrency exception by retrying blindly or by re-reading and forcing the write through. A lost race is a legitimate `409`, not an error to paper over.

**Acceptance for this piece:** you must be able to demonstrate two concurrent create-hold requests for the last ticket where one succeeds and the other receives a conflict, with `AvailableQuantity` never going negative.

---

## 10. Dependency injection registration

In `Program.cs`, register every new type with the correct lifetime (all `Scoped`, matching the repositories and the shared `DbContext`):
- Each service against its interface (`IAuthService`, `IEventService`, `ITicketTypeService`, `IReservationService`).
- The unit-of-work port (or your chosen commit mechanism).
- The password-hasher port and the token-generator port (registered where their implementations live — Infrastructure).

Keep the registrations grouped and commented the same way the repository registrations already are.

---

## 11. Definition of done

- [ ] `dotnet build Seatsure.slnx` is green (or `dotnet build` on each project if you only have the 8.0 SDK).
- [ ] The Application layer has **zero** references to EF Core or ASP.NET types — verify by checking the using-statements.
- [ ] No Domain entity appears in any service signature; only DTOs cross the boundary.
- [ ] Every failure path throws a typed exception from Part 5 — no returning `null` to mean "not found", no booleans to mean "forbidden".
- [ ] `CreateHold` reads a tracked `TicketType`, commits in one `SaveChanges`, and translates `DbUpdateConcurrencyException` into a conflict.
- [ ] Every service and port is registered in DI.
- [ ] A short note or ADR records your commit-mechanism decision and why.

**Explain-back checkpoint (be ready to answer these without notes):**
1. Why do DTOs exist when we already have entities?
2. Why is the commit not inside the repository's `AddAsync`?
3. Walk through exactly what happens in the database when two people reserve the last ticket at the same time.
4. Why is the ownership check on `publish` written in code instead of expressed with a role attribute?

---

## 12. Explicitly out of scope (next tasks — do not start here)

Controllers · JWT Bearer authentication middleware + `[Authorize]` · RFC 7807 Problem Details middleware (the exception→status mapping) · SignalR hub + `AvailabilityChanged` broadcasts · hold-expiry `BackgroundService` · xUnit test project.

Build the services so these drop in cleanly later — but write none of them now.
