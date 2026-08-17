using Seatsure.Domain;

namespace Seatsure.BLL.DTOs.Auth;

// Entities never cross the controller boundary (README §3.5) — these records are the contract.
// Rules, identify what to show, what to hide
// Dto helps in this 

public record RegisterRequest(string Name, string Email, string Password, UserRole Role);

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, DateTime ExpiresAtUtc);

public record UserDto(Guid Id, string Name, string Email, string Role);
