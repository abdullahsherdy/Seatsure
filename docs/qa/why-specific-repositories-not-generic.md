# Why specific repositories instead of a generic IRepository<T>?

## Question
Why not create a generic `IRepository<T>` and have each entity repository inherit from it?

---

## Short Answer
EF Core is already a generic repository. Adding another one on top just wraps a pattern with the same pattern. Specific repositories also follow the Interface Segregation Principle — each interface exposes only what consumers actually need.

---

## The Practical Reason: EF Core already is a generic repository

`DbSet<T>` on `AppDbContext` is literally a generic repository. `DbContext` itself is a Unit of Work. Writing this:

```csharp
public interface IRepository<T>
{
    Task<T?> GetByIdAsync(Guid id);
    Task AddAsync(T entity);
    Task SaveChangesAsync();
}
```

...wraps a pattern with the same pattern — one layer of indirection that does nothing useful. The classic symptom is that `GetAllAsync()` or `Find(Expression<Func<T, bool>> predicate)` ends up leaking `IQueryable<T>` into the service layer. At that point services are still writing LINQ against EF concepts — nothing was actually abstracted.

---

## The Principled Reason: Interface Segregation (SOLID — ISP)

Each repository in this project has domain-specific queries:

```
IUserRepository        → GetByEmailAsync     (login / register duplicate check)
IEventRepository       → GetPublishedAsync   (paged, status-filtered)
IReservationRepository → GetExpiredHoldsAsync (background service)
ITicketTypeRepository  → GetByEventIdAsync   (scoped to a specific event)
```

None of these are interchangeable. If you inherit from `IRepository<T>`:

```csharp
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
}
```

You are now forced to implement `GetAllAsync()` on `UserRepository` even though no endpoint ever lists all users. Your options are all bad:
- `throw new NotImplementedException()` — a lie in the contract
- Return an empty list — wrong behavior
- Implement it and never call it — dead code

**ISP rule:** an interface should not force implementors to depend on methods they don't use.

---

## When would a generic repository make sense?

- Admin CRUD panels where every entity has identical list/create/update/delete
- Frameworks or libraries that need to work with any entity type
- Projects where no entity has domain-specific query needs

This project has real domain queries per aggregate. Specific repositories are the right fit.

---

## Rule of Thumb

| Context | Approach |
|---|---|
| Framework / library | Generic `IRepository<T>` |
| Application with domain logic | Specific repositories per aggregate |

---

## Key Principle Referenced
**SOLID — Interface Segregation Principle (ISP):** Clients should not be forced to depend on methods they do not use. Keep interfaces small and focused.
