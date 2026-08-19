using Seatsure.Domain;

namespace Seatsure.Application.Security;
public interface IJWTService
{
    (string token, DateTime ExpiresAtUtc) generateToken(User user);

}
