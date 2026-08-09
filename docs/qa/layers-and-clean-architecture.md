# Q&A — How to Decide What Goes in Each Layer + Clean Architecture

> **Question:** Explain how to decide what to put in each layer, explain Clean
> Architecture, and does SeatSure follow Clean Architecture or not?

The decision to remove `Seatsure.Application` is *exactly* the fork between the two
architectures. This is built up in three parts.

---

## Part 1 — How to decide what goes in each layer

One question resolves ~90% of "where does this go" decisions:

> **"What kind of change would force me to edit this code?"**

Group code by its *reason to change* (Single Responsibility Principle at the module level).
Four different reasons → four layers:

| Layer | Changes when… | Litmus test | SeatSure examples |
|---|---|---|---|
| **Domain** | the *business concept* changes | "Could this compile with **zero** NuGet packages?" | `TicketType`, `Reservation`, `ReservationStatus`, `RowVersion` as a concept |
| **DAL / Infrastructure** | the *technology* changes (SQL Server → Postgres, EF → Dapper) | "Does this `import` a framework/driver?" | `AppDbContext`, migrations, `UserRepository` impl, EF configs |
| **BLL / Application** | the *business rules / use cases* change | "Would a product manager care about this rule?" | "hold expires in 10 min", "can't oversell", "only organizer can publish" |
| **API / Presentation** | the *delivery mechanism* changes (REST → gRPC) | "Is this about HTTP specifically?" | controllers, status codes, JSON shape, JWT middleware, Swagger |


### The tricky cases (where people guess wrong)

- **DTOs** → Application. They're the *use-case contract*, not the HTTP contract.
- **Validation** splits by *type*:
  - "quantity ≤ available inventory" → **Application** (business rule)
  - "email is well-formed, field is required" → **API** (data annotations)
- **Ownership checks** ("organizer owns this event") → **Application**, because it's a
  business rule, *not* `[Authorize]`. This is the "authz vs ownership" lesson in README §6.
- **Password hashing / JWT issuing** → technically infrastructure (it's a technology), but
  the *interface* (`IPasswordHasher`, `ITokenService`) is an application port. The plan's
  compromise — abstractions in `BLL/Security` — is pragmatic and fine at this size.
- **Exceptions** (`ConflictException`) → Application or Domain, never Infrastructure. They
  express business outcomes ("someone booked first"), not EF errors.

---

## Part 2 — What Clean Architecture actually is

Clean Architecture (Robert C. Martin) is **concentric circles**, not a vertical stack:

```
        ┌──────────────────────────────────────┐
        │  Frameworks & Drivers                │  ← EF Core, ASP.NET, SQL Server
        │   ┌─────────────────────────────┐    │
        │   │  Interface Adapters         │    │  ← Controllers, Repository IMPLs
        │   │   ┌─────────────────────┐   │    │
        │   │   │  Use Cases          │   │    │  ← Services (Application rules)
        │   │   │   ┌─────────────┐   │   │    │
        │   │   │   │  Entities   │   │   │    │  ← Domain
        │   │   │   └─────────────┘   │   │    │
        │   │   └─────────────────────┘   │    │
        │   └─────────────────────────────┘    │
        └──────────────────────────────────────┘
```

The **entire architecture is one rule** — *The Dependency Rule*:

> **Source-code dependencies may only point INWARD.** Nothing in an inner circle knows
> anything about an outer circle.

The consequence that trips everyone up: the **database is the outermost circle**. It's a
detail, like the web framework. So the infrastructure must depend *inward* on the
application core — **the core must NOT depend on infrastructure.**

But your use cases obviously need to *load* a `TicketType` from somewhere. How can
Application not depend on the DAL that fetches it? **Dependency Inversion:**

- **Application** *defines* the interface: `ITicketTypeRepository` (a "port").
- **Infrastructure** *implements* it: `TicketTypeRepository : ITicketTypeRepository`.
- So the arrow points `Infrastructure → Application`, not the other way.

The interface lives with the code that *needs* it (the use case), not with the code that
*fulfills* it (EF). That inverted arrow is the whole trick.

---

## Part 3 — Does SeatSure follow Clean Architecture?

**No. It's a traditional N-tier / layered architecture.** Here's the proof, from the
dependency chain:

```
Seatsure → Seatsure.BLL → Seatsure.DAL → Seatsure.Domain
                    └──────────┘
                    BLL depends on DAL
```

**BLL (use cases) depends on DAL (infrastructure).** That single arrow points *outward*
toward the database — the exact opposite of the Dependency Rule. Because the repository
interfaces live *inside* `Seatsure.DAL`, the application core transitively references EF
Core. Clean Architecture requires that arrow **reversed**.

### The irony: the project *had* Clean Architecture, and we removed it

The original phase-01 plan put repository **interfaces** in `Seatsure.Application` and
**implementations** in `Seatsure.DAL`, with `DAL → Application`. That inverted arrow **was**
Clean Architecture (Ports & Adapters). Collapsing `Seatsure.Application` into DAL
deliberately traded Clean Architecture for simpler traditional layering.

### What it would take to be "Clean" again

Move just the **interfaces** (not implementations) into an inward project:

```
Seatsure.Domain                    ← entities
Seatsure.Application               ← repo INTERFACES + service interfaces + DTOs + use cases
Seatsure.Infrastructure (was DAL)  ← DbContext + repo IMPLEMENTATIONS  →  references Application
Seatsure.Api                       ← controllers  →  references Application (Infra only at Program.cs)
```

Dependencies now all point inward: `Api → Application`, `Infrastructure → Application →
Domain`. Nothing points at Infrastructure except the composition root (`Program.cs`), which
is *allowed* to be "dirty" because it's the wiring edge.

### Does it matter for you? Honestly — no.

| | Traditional layered (current) | Clean Architecture |
|---|---|---|
| Testability of services | ✅ Yes — you mock `IReservationRepository` | ✅ Yes |
| Swap EF for Dapper without touching services | ⚠️ Interface exists, but BLL still *references* EF | ✅ Core never sees EF |
| Project count / ceremony | Lower | Higher |
| Right for a **teaching project about concurrency** | ✅ | Overkill |

You still get the payoff that matters — the `IReservationRepository` seam means the
`ReservationService` concurrency tests can mock the repo. The *only* thing lost is a
compile-time guarantee that EF types can't leak into services. For SeatSure, whose whole
teaching point is the `RowVersion` → `409` mechanism (README §4), that guarantee isn't worth
two extra projects.

**Verdict:** this is pragmatic **layered architecture with dependency injection** — the
sensible 80/20 of Clean Architecture. Call it "Clean-*inspired*," not Clean Architecture. If
a later phase's goal becomes "teach Clean Architecture" specifically, the refactor above is
small and mechanical: move interfaces inward, rename DAL → Infrastructure, flip one
reference.


# Presentation -> Application -> Infra ->domain 

## Entities (Models)
## external resources api, databases
## business logic
## presentation -> present api's 

## 1. Models -> has an interact or direct depenedency on other things 
