using Seatsure.BLL.DTOs.Auth;
using Seatsure.BLL.Exceptions;
using Seatsure.BLL.Security;
using Seatsure.BLL.Services.Interfaces;
using Seatsure.DAL;
using Seatsure.DAL.Repositories.Interfaces;
using Seatsure.Domain;

namespace Seatsure.BLL.Services;

internal sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;

    public AuthService(IUserRepository users, IUnitOfWork unitOfWork, IPasswordHasher hasher, ITokenService tokens)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<UserDto> RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Name is required.");
        if (string.IsNullOrWhiteSpace(email))
            throw new ValidationException("Email is required.");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            throw new ValidationException("Password must be at least 6 characters.");
        if (!Enum.IsDefined(request.Role))
            throw new ValidationException("Role is invalid.");

        // 409 on duplicate email (README §3.1). Unique index on User.Email is the backstop.
        if (await _users.GetByEmailAsync(email) is not null)
            throw new ConflictException("Email is already registered.");

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = _hasher.Hash(request.Password),
            Role = request.Role,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return new UserDto(user.Id, user.Name, user.Email, user.Role.ToString());
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _users.GetByEmailAsync(email);

        // Same error whether the email is unknown or the password is wrong — don't leak which.
        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException();

        var (token, expiresAtUtc) = _tokens.GenerateToken(user);
        return new LoginResponse(token, expiresAtUtc);
    }
}
