using Seatsure.BLL.DTOs.Auth;

namespace Seatsure.BLL.Services.Interfaces;

public interface IAuthService
{
    Task<UserDto> RegisterAsync(RegisterRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);
}


// Business rule
// Business -> no generic 
