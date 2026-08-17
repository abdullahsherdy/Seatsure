namespace Seatsure.BLL.Security;

/// <summary>Password hashing via BCrypt (work factor pinned by the library default of 11).</summary>
internal sealed class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
