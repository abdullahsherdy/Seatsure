# Session Discussion Guide — Arguing the "Why" Behind SeatSure

> **Purpose:** Socratic debate prompts for the service-layer session. These are grenades to throw into the room, not lectures. Every prompt is grounded in *this* codebase — the anemic entities, `TicketType.RowVersion`, the `AddAsync`-only repositories, and the services students are about to build.

> **Companion task:** [services.md](services.md) — students implement while these debates run alongside.

---

## How to run these

1. **Don't reveal "Where I land" first.** Ask the question, let a student commit to a position out loud, then have another student attack it.
2. **Reject appeals to authority.** "Clean architecture says so" / "the skill says so" is not an argument. Make them reason from consequences.
3. **The win condition** is a student changing their mind out loud *and* being able to say why — not arriving at the "right" answer.
4. **Ground every abstract claim in a line of our code.** If they can't point to where in SeatSure a principle lives or breaks, they're reciting definitions.

Each prompt has: **Provoke** (the opener) · **The argument** (both sides) · **Where I land** (senior judgment + tradeoff) · **Push further** (a follow-up gotcha).

---

## Theme A — Understanding the business before the code

### A1. "You can't write `IReservationService` until you can tell me the one sentence this whole app exists to protect."
- **Provoke:** Before anyone opens an editor — what is the single invariant SeatSure must never violate? One sentence on the board.
- **The argument:** Students say "let people book tickets." Push back: that's a *feature*, not an *invariant*. The invariant is **"a ticket type is never sold beyond its capacity — `AvailableQuantity` never goes negative, even under a race."** Everything else (auth, events, JWT) is scaffolding around protecting that one number.
- **Where I land:** The business rule *drives the architecture*. `RowVersion` exists **because** of that sentence. If they can't name the invariant, they'll build the plumbing and miss the point.
- **Push further:** "Where does that invariant actually get enforced — controller, service, entity, or database?" (It's defended in *layers*; the last line of defense is the DB via `RowVersion`. Segue to Theme G.)

### A2. "Read me the lifecycle of a Reservation. If you can't draw the state machine, you don't understand the domain."
- **Provoke:** Draw the states of a `Reservation` and every legal transition. Then do the same for `Event`.
- **The argument:** They'll list `Pending → Confirmed`. Ask about `Cancelled`, `Expired`. Who triggers each? (User confirms, user cancels, *the system* expires via the background job.) Which transitions are illegal — confirm an expired hold? cancel a confirmed one?
- **Where I land:** `ReservationStatus` / `EventStatus` encode a **state machine**, and half the business rules are just "which transitions are legal." A method like `Confirm` is mostly a guard on the current state.
- **Push further:** "Your enum lets any value sit in any row. What stops the DB from holding a nonsense transition like Confirmed→Pending? Nothing in the type system does — so *where* does that rule live?" (Sets up C1.)

---

## Theme B — Why a service layer at all

### B1. "The service layer is just a pointless middleman. Prove me wrong."
- **Provoke:** Take the wrong senior position deliberately: "Controllers can call repositories directly. A service that just calls `repo.AddAsync` then `SaveChanges` adds a file and a layer for nothing. Delete it."
- **The argument:** Weak rebuttal: "clean architecture says so" — don't accept it. Strong rebuttal: put the business logic *somewhere* and watch the middleman argument collapse — *where does the ownership check on `publish` go? the concurrency `try/catch`? "hash password → create user → commit, atomically"?* A controller doing that can't be reused by a SignalR hub or a background job, and can't be unit-tested without spinning up HTTP.
- **Where I land:** A pass-through service *is* waste — but SeatSure's services aren't pass-throughs; they're where **transactions, invariants, and cross-repository orchestration** live. The tell: a method touches *two* repositories in one commit (reservation + ticket type) — that coordination has no home in a controller or a single repo.
- **Push further:** "The .NET expert playbook pushes MediatR/CQRS instead of hand-written services. Ours uses plain services. Was that a mistake?" (Tradeoff: MediatR gives pipeline behaviors + one-handler-per-use-case SRP, at the cost of indirection and a dependency. For an 8-session teaching project, plain services are the *right* boring choice. Make them defend "boring.")

---

## Theme C — OOP and the anemic domain model (the sharpest one for our code)

### C1. "Your services are about to violate encapsulation, and you're calling it clean architecture."
- **Provoke:** The big one. `TicketType` is a bag of public setters. `ReservationService.CreateHold` will do `ticketType.AvailableQuantity -= quantity` **from outside the object**. "Is that object-oriented? Or procedural code wearing a class costume?"
- **The argument:**
  - *Anemic-model defenders (mainstream .NET):* entities are persistence shapes; logic lives in services; simple, EF-friendly, everyone does it.
  - *Rich-domain defenders (DDD / Fowler):* an object that can't protect its own invariant isn't encapsulated. `TicketType` should expose `Reserve(int quantity)` that throws if `quantity > AvailableQuantity` and decrements internally. The service *orchestrates*; the entity *enforces*.
- **Where I land:** Fowler literally calls the anemic domain model an **anti-pattern** — the point of objects is bundling data with the rules that guard it. "Never oversell" is a property of a `TicketType`; it belongs *on* `TicketType`. But be honest: pushing behavior into entities fights EF's tracking model and adds ceremony, so mainstream .NET accepts anemic entities as a pragmatic default. The lesson: **know you're making the trade; don't make it by accident.**
- **Push further:** "If `TicketType.Reserve()` enforced the check, would you still need the service's inventory guard? Would you still need `RowVersion`?" (Killer: the in-memory guard moves onto the entity, but `RowVersion` is a *database* concurrency concern — no OOP fixes a race between two processes. **Encapsulation and concurrency are different axes.** A genuinely deep realization.)

### C2. "Tell me the difference between a rule you can break at compile time and one you can only break at runtime."
- **Provoke:** `AvailableQuantity` is an `int` — nothing stops `-5`. `ReservationStatus` is an enum — nothing stops an illegal transition. Which invariants does the *type system* protect, and which are you protecting by hand?
- **The argument:** Connect to "make illegal states unrepresentable." Could `AvailableQuantity` be a type that can't go negative? Could reservation states be a shape where `ConfirmedAtUtc` only *exists* in the Confirmed state, instead of a nullable field that's meaningless otherwise?
- **Where I land:** C# is weaker at this than F#/TS unions, so .NET leans on runtime guards + exceptions. But the mindset matters: every nullable field that's "only set in some states" (`ConfirmedAtUtc`, `HoldExpiresAtUtc`) is a small lie the type system tells. Recognizing that is senior-level even when you choose not to fix it.
- **Push further:** "Your DTOs are records with non-nullable properties. Did you move validation to the boundary — 'parse, don't validate' — or leave a hole?" (Segue to D.)

---

## Theme D — Why `record` for DTOs

### D1. "Why records? And why is using a record for your EF entity a trap?"
- **Provoke:** "You were told to use `record` for DTOs. Justify it in terms of *behavior*, not because a rule said so. Then tell me why I should **not** make `TicketType` a record."
- **The argument:** A DTO is a **value** — two DTOs with the same fields *are* the same thing; immutable once across the boundary; `with`-expressions make safe copies. Records give value equality + immutability for free. An **entity** has *identity* — two `TicketType`s with identical fields but different `Id` are different rows; it's mutable (the whole point of change tracking); record value-equality actively fights EF's identity-based tracking. Same keyword, opposite semantics.
- **Where I land:** Records for DTOs matches the tool to the semantic: **value semantics for data-in-transit, reference+identity semantics for tracked entities.** The `RowVersion` on `TicketType` is the giveaway — an entity carries mutable state that changes over its lifetime; a record models a frozen snapshot.
- **Push further:** "Your `AuthResultDto` holds a token. Should a record hold a secret? What are the equality and `ToString()` implications of putting a JWT in a record?" (Records auto-generate a `ToString()` that prints every property — a real footgun for logging secrets. Ties to the security rule "never log tokens.")

---

## Theme E — SOLID, grounded in SeatSure (not textbook)

Run these as "find the violation in *our* code," never as definitions.

### E1. SRP — "Is `IReservationService` doing four jobs?"
- **Provoke:** It holds `CreateHold`, `Confirm`, `Cancel`, `MyReservations`. One responsibility or four? Split it?
- **Where I land:** SRP is "one *reason to change*," not "one method." All four change together when *reservation rules* change → one service is correct. Contrast with a class that did reservations *and* sent emails *and* issued tokens — different reasons to change, split it. The line is the *axis of change*, and students usually draw it too aggressively.

### E2. DIP — "Point to the dependency inversion in this repo. It's not where you think."
- **Provoke:** Where is DIP physically visible in the folder structure?
- **Where I land:** Repository *interfaces live in `Application`* (inner layer defines the port); *implementations live in `Infrastructure`* (outer layer supplies the adapter). The arrow points **inward** — Infrastructure depends on Application, not the reverse. DIP made structural, not just "inject an interface." The `IUnitOfWork` decision is the same move: Application declares "I need a way to commit," Infrastructure decides it's EF.
- **Push further:** "If DIP is so great, why does EF's `DbContext` — a concretion — get away with being a de-facto Unit of Work? Are we inverting a dependency or just wrapping one we can't escape?" (→ E4.)

### E3. ISP / LSP — "Could you really swap EF for Dapper behind `IReservationRepository`? Be honest."
- **Provoke:** We claim the repo interfaces let us swap the data layer. Prove it — or admit it's a comforting lie.
- **Where I land:** Partly a lie, and seniors say so. The interfaces *look* clean, but the concurrency behavior **leaks**: the design depends on `FindAsync` returning a *tracked* entity so `RowVersion` works. A Dapper implementation has no change tracker — it satisfies the interface's *signature* (LSP on types) but violates its *contract* (LSP on behavior). The abstraction is leaky, and pretending otherwise is how you get burned.
- **Push further:** "So is the repository abstraction earning its keep, or is it ceremony over EF?" → E4.

### E4. "Repository + UnitOfWork over EF Core is redundant. Defend it or drop it."
- **Provoke:** `DbContext` is *already* a repository (`DbSet<T>`) and *already* a unit of work (`SaveChanges`). Wrapping it in `IRepository` + `IUnitOfWork` re-implements what you have. Over-engineering — cut it.
- **The argument:** *For cutting:* less code, no leaky abstraction, EF is testable now with in-memory/SQLite. *For keeping:* the boundary keeps EF out of `Application`, makes domain logic mockable without a database, and gives one honest name — `SaveChangesAsync` on a UoW — instead of scattering `context.SaveChanges` across services.
- **Where I land:** Both are *defensible*; the wrong answer is doing it *without knowing why*. For SeatSure the teaching value of the port outweighs the redundancy — but a senior on a small real project might legitimately say "just use `DbContext` directly and stop pretending." Make them own the tradeoff.

---

## Theme F — Exceptions vs Result types

### F1. "The type-safety school says 'Result types over exceptions.' Your task uses exceptions. One of us is wrong."
- **Provoke:** Put the conflict on the table. The Result school says errors should be *visible in the signature* — `Result<T, Error>` — so callers can't forget them. Your services throw `NotFoundException`, `ConflictException`. Who's right?
- **The argument:**
  - *Result camp:* "insufficient inventory" is an *expected* outcome, not exceptional. Hiding it in a thrown exception means the compiler never forces the controller to handle it; you find out at runtime.
  - *Exception camp (.NET idiom):* exceptions keep the happy path clean, integrate with ASP.NET middleware → one place maps exception-type→HTTP status (RFC 7807), and the framework, EF (`DbUpdateConcurrencyException`!), and the BCL already speak exceptions.
- **Where I land:** A *values* difference, not right/wrong. The honest test: **is this failure expected and common, or genuinely exceptional?** A concurrency loss on the last ticket is arguably *expected* → a Result models it more truthfully. A missing event by id is closer to exceptional. .NET's ecosystem gravity makes exceptions the pragmatic default *here*, and consistency beats mixing paradigms — but a student who argues for `Result` on `CreateHold` specifically is making a *good* argument.
- **Push further:** "EF hands you `DbUpdateConcurrencyException` whether you like it or not. Does that settle the debate for the reservation flow, or just for that one boundary?" (It settles the *boundary* — you catch EF's exception — but you could still translate it to a `Result` at the service edge.)

---

## Theme G — The concurrency debate (the heart of it)

### G1. "Why optimistic locking? A lock would be simpler and it'd never oversell."
- **Provoke:** "Just take a lock on the `TicketType` row (pessimistic), decrement, release. No `RowVersion`, no retries, no 409. Why do the hard thing?"
- **The argument:** *Pessimistic:* correct, easy to reason about — but *serializes* every buyer for a hot event, holds DB locks across the request, risks deadlocks/timeouts exactly when you're busiest. *Optimistic (`RowVersion`):* assumes conflicts are *rare*, lets everyone proceed in parallel, only the loser pays (409 → retry). For ticketing, reads and non-conflicting writes vastly outnumber true collisions, so optimistic wins on throughput.
- **Where I land:** Optimistic concurrency is a **bet that conflicts are rare**. Right for most inventory; worst case for the *last* ticket of a hyped event (everyone collides). Seniors pick the model from the *contention profile*, not habit. `RowVersion` is the right default here and the 409 is a *feature* — the system telling the truth: "someone beat you, try again."
- **Push further:** "You could skip `RowVersion` entirely with one statement: `UPDATE ... SET Available = Available - @q WHERE Id = @id AND Available >= @q`, then check rows-affected. No token, no tracked read. Why doesn't SeatSure do that?" (The atomic UPDATE is arguably *better* for the pure decrement — but the flow also **creates a Reservation row and mutates other tracked state in the same transaction**, and EF wants a tracked entity. `RowVersion` generalizes to "any change to this row conflicts," not just the quantity. Make them feel *why* the general tool beat the clever one-liner — and admit the one-liner is legitimately good.)

### G2. "Where does `UtcNow` come from, and why should I be suspicious of it?"
- **Provoke:** `HoldExpiresAtUtc = UtcNow + 10 min`. The background job compares against `UtcNow`. "What's wrong with calling `DateTime.UtcNow` directly inside the service?"
- **Where I land:** An untestable hidden dependency on the system clock — you can't unit-test expiry without waiting or hacking the OS clock. A senior injects an `IClock`/`TimeProvider` (.NET 8 has `TimeProvider`) so time is a *parameter*, not ambient global state. And everything is UTC for a reason — the `Utc` suffix is a discipline against the timezone bugs that eat juniors alive.
- **Push further:** "If a hold expires the same millisecond the user confirms, who wins — the confirm or the expiry job? Is that a race too?" (Yes — a second concurrency front, this time between the request and the `BackgroundService`. `RowVersion` mediates it. Ties the theme together.)

---

## Theme H — Two more sharp ones

### H1. "You used `Guid` ids and ignored the README's `int`. Justify it, then tell me the cost."
- **Provoke:** Why `Guid` over `int` identity?
- **Where I land:** *For Guid:* generatable before insert (no round-trip for the id), non-guessable (an `int` id leaks how many events exist and invites enumerating `/events/1,2,3…` — an IDOR/enumeration smell), merge-friendly across databases. *Cost:* 16 bytes vs 4, no natural sort order, and **random Guids fragment the clustered index** and bloat every foreign key. Senior fix if it matters: sequential/`NEWSEQUENTIALID`-style Guids for uniqueness *and* index locality.
- **Push further:** "Does a non-guessable id mean you can skip authorization checks on `cancel`?" (No — never. Obscurity isn't access control. The ownership check stays. A security reflex worth drilling.)

### H2. "Thin controllers, fat services — until the service becomes the new God class. Where's the line?"
- **Provoke:** We pushed logic out of controllers into services to keep controllers thin. What stops `EventService` from becoming a 2,000-line dumping ground?
- **Where I land:** The same SRP axis-of-change test (E1), plus a smell: when a service method stops *orchestrating* and starts *computing complex domain rules*, that logic wants to move down onto the entity (back to C1) or into a domain service. "Thin controllers" is not "fat services" — it's "logic lives at the *right altitude*," and the right altitude for an invariant is usually lower than students put it.

---

## Suggested weighting

If time is short, lead hard with the two that best separate students who *get* it from those reciting definitions:
- **C1** — the anemic-domain / encapsulation fight.
- **G1** — `RowVersion` vs the clever one-line atomic UPDATE.

Both force students off memorized definitions and into reasoning about *this* system's real tradeoffs.
