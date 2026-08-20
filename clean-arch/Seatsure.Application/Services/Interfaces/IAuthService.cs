
//using Seatsure.Domain;
using Seatsure.Application.DTOs.Auth;
namespace Seatsure.Application.Services.Interfaces;

/*
 *  ### 8.1 `IAuthService`
- **Register:** validate the input (email format/uniqueness intent, password present, valid role). 
Reject a duplicate email with the email-taken exception.
Hash the password via the port. 
Create the `User` (set `CreatedAtUtc` in UTC), 
stage it via the user repository, commit. 
Return the created user's public data (never the hash).
- **Login:** look up the user by email; if missing or the password fails verification, throw the same failure for both cases (do not reveal which was wrong — an auth best practice). On success, issue a token via the token-generator port and return the token + `expiresAtUtc`.

 */
public interface IAuthService
{

    Task<UserDto> Register(RegisterRequest request);

    Task<AuthResult> Login(LoginRequest request);


}