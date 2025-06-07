using Nemui.Shared.DTOs.Auth;

namespace Nemui.Application.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<bool> LogoutAsync(string refreshToken);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);
}