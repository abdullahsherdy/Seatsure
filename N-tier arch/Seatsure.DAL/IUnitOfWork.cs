namespace Seatsure.DAL;

/// <summary>
/// The request-scoped transaction boundary. Services commit through this single seam instead
/// of calling <c>SaveChangesAsync</c> on an individual repository — all repositories share one
/// scoped <see cref="AppDbContext"/>, so one commit here persists every tracked change together.
/// </summary>
/// 
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
