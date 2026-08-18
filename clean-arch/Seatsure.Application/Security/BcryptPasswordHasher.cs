
namespace Seatsure.Application.Security;

internal sealed class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string Password) => BCrypt.Net.BCrypt.HashPassword(Password);

    public bool verify (string password, string hashedPassword) => BCrypt.Net.BCrypt.Verify(password, hashedPassword);)
}

