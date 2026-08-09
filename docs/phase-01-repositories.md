# Phase 1 — Repository Pattern

## Step 1 — Create `Seatsure.Application` project

In Visual Studio: right-click solution → **Add → New Project → Class Library (.NET 8.0)** → name `Seatsure.Application`.

Delete the auto-generated `Class1.cs`. Create this folder inside it:
```
Seatsure.Application/Repositories/
```

---

## Step 2 — Wire up project references

```bash
dotnet add Seatsure.Application/Seatsure.Application.csproj reference Seatsure.Domain/Seatsure.Domain.csproj
dotnet add Seatsure.DAL/Seatsure.DAL.csproj reference Seatsure.Application/Seatsure.Application.csproj
dotnet add Seatsure/Seatsure.csproj reference Seatsure.Application/Seatsure.Application.csproj
```

Or via VS: right-click each project → **Add → Project Reference**.

---

## Step 3 — Repository interfaces (`Seatsure.Application/Repositories/`)

**IUserRepository.cs**
```csharp
using Seatsure.Domain;

namespace Seatsure.Application.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task AddAsync(User user);
    Task SaveChangesAsync();
}
```

**IEventRepository.cs**
```csharp
using Seatsure.Domain;

namespace Seatsure.Application.Repositories;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id);
    Task<(IEnumerable<Event> Items, int TotalCount)> GetPublishedAsync(int page, int pageSize);
    Task AddAsync(Event ev);
    Task SaveChangesAsync();
}
```

**ITicketTypeRepository.cs**
```csharp
using Seatsure.Domain;

namespace Seatsure.Application.Repositories;

public interface ITicketTypeRepository
{
    Task<TicketType?> GetByIdAsync(Guid id);
    Task<IEnumerable<TicketType>> GetByEventIdAsync(Guid eventId);
    Task AddAsync(TicketType ticketType);
    Task SaveChangesAsync();
}
```

**IReservationRepository.cs**
```csharp
using Seatsure.Domain;

namespace Seatsure.Application.Repositories;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(Guid id);
    Task<IEnumerable<Reservation>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<Reservation>> GetExpiredHoldsAsync();
    Task AddAsync(Reservation reservation);
    Task SaveChangesAsync();
}
```

---

## Step 4 — Implementations (`Seatsure.DAL/Repositories/`)

**UserRepository.cs**
```csharp
using Microsoft.EntityFrameworkCore;
using Seatsure.Application.Repositories;
using Seatsure.Domain;

namespace Seatsure.DAL.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context) => _context = context;

    public async Task<User?> GetByIdAsync(Guid id) =>
        await _context.Users.FindAsync(id);

    public async Task<User?> GetByEmailAsync(string email) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task AddAsync(User user) =>
        await _context.Users.AddAsync(user);

    public async Task SaveChangesAsync() =>
        await _context.SaveChangesAsync();
}
```

**EventRepository.cs**
```csharp
using Microsoft.EntityFrameworkCore;
using Seatsure.Application.Repositories;
using Seatsure.Domain;

namespace Seatsure.DAL.Repositories;

public class EventRepository : IEventRepository
{
    private readonly AppDbContext _context;

    public EventRepository(AppDbContext context) => _context = context;

    public async Task<Event?> GetByIdAsync(Guid id) =>
        await _context.Events
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.Id == id);

    public async Task<(IEnumerable<Event> Items, int TotalCount)> GetPublishedAsync(int page, int pageSize)
    {
        var query = _context.Events.Where(e => e.Status == EventStatus.Published);
        var total = await query.CountAsync();
        var items = await query
            .OrderBy(e => e.StartsAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, total);
    }

    public async Task AddAsync(Event ev) =>
        await _context.Events.AddAsync(ev);

    public async Task SaveChangesAsync() =>
        await _context.SaveChangesAsync();
}
```

**TicketTypeRepository.cs**
```csharp
using Microsoft.EntityFrameworkCore;
using Seatsure.Application.Repositories;
using Seatsure.Domain;

namespace Seatsure.DAL.Repositories;

public class TicketTypeRepository : ITicketTypeRepository
{
    private readonly AppDbContext _context;

    public TicketTypeRepository(AppDbContext context) => _context = context;

    public async Task<TicketType?> GetByIdAsync(Guid id) =>
        await _context.TicketTypes.FindAsync(id);

    public async Task<IEnumerable<TicketType>> GetByEventIdAsync(Guid eventId) =>
        await _context.TicketTypes
            .Where(t => t.EventId == eventId)
            .ToListAsync();

    public async Task AddAsync(TicketType ticketType) =>
        await _context.TicketTypes.AddAsync(ticketType);

    public async Task SaveChangesAsync() =>
        await _context.SaveChangesAsync();
}
```

**ReservationRepository.cs**
```csharp
using Microsoft.EntityFrameworkCore;
using Seatsure.Application.Repositories;
using Seatsure.Domain;

namespace Seatsure.DAL.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly AppDbContext _context;

    public ReservationRepository(AppDbContext context) => _context = context;

    public async Task<Reservation?> GetByIdAsync(Guid id) =>
        await _context.Reservations
            .Include(r => r.TicketType)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<IEnumerable<Reservation>> GetByUserIdAsync(Guid userId) =>
        await _context.Reservations
            .Where(r => r.UserId == userId)
            .ToListAsync();

    public async Task<IEnumerable<Reservation>> GetExpiredHoldsAsync() =>
        await _context.Reservations
            .Include(r => r.TicketType)
            .Where(r => r.Status == ReservationStatus.Pending && r.HoldExpiresAtUtc < DateTime.UtcNow)
            .ToListAsync();

    public async Task AddAsync(Reservation reservation) =>
        await _context.Reservations.AddAsync(reservation);

    public async Task SaveChangesAsync() =>
        await _context.SaveChangesAsync();
}
```

---

## Step 5 — Register in DI (`Program.cs`)

```csharp
using Seatsure.Application.Repositories;
using Seatsure.DAL.Repositories;

// add before builder.Build()
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<ITicketTypeRepository, TicketTypeRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
```

---

## Step 6 — Build

```bash
dotnet build
```

Expected: 0 errors.
