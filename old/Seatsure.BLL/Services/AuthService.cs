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

    public AuthService(AppDbContext dbContext, IUserRepository users, IUnitOfWork unitOfWork, IPasswordHasher hasher, ITokenService tokens)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _hasher = hasher;
        _tokens = tokens;
    }

    /*  
         think as stackholders 

        Register 
        1. add new user 
        2/ user register using RegisterRequest -> Dto
        
            public record RegisterRequest(string Name, string Email, string Password, UserRole Role);
        3. name, email, password, role are required 
        4. passwrod -> hashing 

        // validation
            1. user email donesn't exit before. 
            2. validate user password -> legnth 
            3. name not userName, uniquness (name+email), userName critical 
            4. 
    */

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

        /*
        // valdiate on email 
        // get email from dto 
        var email = request.Email.Trim().ToLowerInvariant(); // user may enter uppercase by mistake 

        // valdiate email, null, done'st esit, add user, save changes, return Dto (indicate sucess adding)

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Name is required.");
        if (string.IsNullOrWhiteSpace(email))
            throw new ValidationException("Email is required.");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            throw new ValidationException("Password must be at least 6 characters.");
        if (!Enum.IsDefined(request.Role))
            throw new ValidationException("Role is invalid.");
        
        // check if user exist before or not. 
        // user -> defined using his email 
        // email, exist; then user exist 

        // using dbcontext 
        // find ? 
        // select ?
        // where 
        // return type 
        // violation ? 
        // _dbContext.Users.Select( u => u.Email == email); 

        // using userRepsitory 

        
        // user exist before, can't register two users with the same email 
        if (_dbContext.Users.FirstOrDefault(u => u.Email == email) is not null){
            
            throw new ConflictException("Email is already registered.");
        }

        var user = new User{
            Name = request.Name,
            Email = request.Email.Trim(),
        }

        _dbContext.Users.Add(user);
        _dbContext.SaveChanges();

        // _dbContext.Events.Delete(); to avoid this operation 
        // Depend on Repository, looks independ, tight coupling 
        // userServices, can access other entities 
        // voilation 
        // code work !, everything is okay, 
        // deisgn maintable, testable

    
        return userDto{};
        */
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
