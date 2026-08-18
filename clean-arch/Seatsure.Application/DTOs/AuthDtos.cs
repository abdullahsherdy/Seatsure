using Seatsure.Domain;

// one file contains multiple DTOs, to list any one use direcly Auth.DtoName 
namespace Seatsure.Application.DTOs.Auth;

public record RegisterRequest(string name, string email, string password, UserRole role);

public record LoginRequest(string email, string password);

public record AuthResult(string token, DateTime expiresAtutc); // frontend recieve this request to either delete cookies after the time, or ask for refereshToken. 

public record UserDto(Guid Id, string name, string email, UserRole role); // will be usable for frontend to display user information, and also for authorization purposes.

