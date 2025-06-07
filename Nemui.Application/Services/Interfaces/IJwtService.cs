using System.Security.Claims;
using Nemui.Shared.Entities;

namespace Nemui.Application.Services.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Task<bool> ValidateTokenAsync(string token);
    Task<ClaimsPrincipal?> ValidateTokenAndGetPrincipalAsync(string token);
    Task<Guid?> GetUserIdFromTokenAsync(string token);
    DateTime GetTokenExpirationTime();
}