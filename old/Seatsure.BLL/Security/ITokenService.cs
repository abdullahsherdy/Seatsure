using Seatsure.Domain;

namespace Seatsure.BLL.Security;

public interface ITokenService
{
    /// <summary>Issues a signed JWT for the user. Claims: sub (id), email, role.</summary>
    (string Token, DateTime ExpiresAtUtc) GenerateToken(User user);
}
