using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nemui.Application.Services;
using Nemui.Infrastructure.Configurations;
using Nemui.Shared.Constants;
using Nemui.Shared.Entities;

namespace Nemui.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;
    private readonly IJwtBlacklistService _jwtBlacklistService;

    public JwtService(IOptions<JwtSettings> jwtSettings, IJwtBlacklistService jwtBlacklistService)
    {
        _jwtBlacklistService = jwtBlacklistService;
        _jwtSettings = jwtSettings?.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
    }

    public string GenerateAccessToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new(AuthConstants.ClaimTypes.EmailVerified, user.IsEmailVerified.ToString()),
            new(AuthConstants.ClaimTypes.JwtId, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationInMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            if (await _jwtBlacklistService.IsTokenBlacklistedAsync(token)) return false;
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = _jwtSettings.CreateTokenValidationParameters();
            var result = await tokenHandler.ValidateTokenAsync(token, validationParameters);
            return result.IsValid;
        }
        catch
        {
            return false;
        }
    }
    
    public async Task<ClaimsPrincipal?> ValidateTokenAndGetPrincipalAsync(string token)
    {
        try
        {
            if (await _jwtBlacklistService.IsTokenBlacklistedAsync(token)) return null;
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = _jwtSettings.CreateTokenValidationParameters();
            var result = await tokenHandler.ValidateTokenAsync(token, validationParameters);
            
            if (result.IsValid && result.SecurityToken is JwtSecurityToken)
            {
                var claimsIdentity = new ClaimsIdentity(
                    result.Claims.Select(c => new Claim(c.Key, c.Value?.ToString() ?? string.Empty)), 
                    "jwt");
                return new ClaimsPrincipal(claimsIdentity);
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<Guid?> GetUserIdFromTokenAsync(string token)
    {
        try
        {
            if (await _jwtBlacklistService.IsTokenBlacklistedAsync(token)) return null;
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = _jwtSettings.CreateTokenValidationParameters();

            var result = await tokenHandler.ValidateTokenAsync(token, validationParameters);
            if (!result.IsValid) return null;

            var userIdClaim = result.Claims.FirstOrDefault(x => x.Key == ClaimTypes.NameIdentifier).Value?.ToString();
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
        catch
        {
            return null;
        }
    }

    public DateTime GetTokenExpirationTime() => DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationInMinutes);
}