namespace Seatsure.DAL;

/// <summary>
/// EF Core's <see cref="AppDbContext"/> already *is* a Unit of Work; this makes that explicit.
/// For now it is a plain forwarder: translating <see cref="Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException"/>
/// into a domain ConflictException cannot happen here, because that exception type lives in
/// Seatsure.BLL and DAL must not reference BLL. So ReservationService keeps that catch — until
/// Clean Architecture inverts the dependency and this becomes the translation seam.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context) => _context = context;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
