using Nemui.Shared.DTOs.Auth;

namespace Nemui.Application.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<bool> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<bool> RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<bool> RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> BlacklistAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default);
}