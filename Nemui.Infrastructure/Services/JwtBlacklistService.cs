using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nemui.Application.Services;
using Nemui.Infrastructure.Configurations;

namespace Nemui.Infrastructure.Services;

public class JwtBlacklistService : IJwtBlacklistService
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<JwtBlacklistService> _logger;
    private readonly RedisSettings _redisSettings;
    private readonly JwtSettings _jwtSettings;

    public JwtBlacklistService(ICacheService cacheService, ILogger<JwtBlacklistService> logger,  IOptions<RedisSettings> redisSettings, IOptions<JwtSettings> jwtSettings)
    {
        _cacheService = cacheService;
        _logger = logger;
        _redisSettings = redisSettings?.Value ?? throw new ArgumentNullException(nameof(redisSettings));
        _jwtSettings = jwtSettings?.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
    }

    public async Task BlacklistTokenAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Attempted to blacklist empty or null token");
            return;
        }

        try
        {
            var jti = ExtractJtiFromToken(token);
            if (string.IsNullOrEmpty(jti))
            {
                _logger.LogWarning("Cannot blacklist token without JTI claim");
                return;
            }
            var expiry = GetTokensRemainingTime(token);
            if (expiry <= TimeSpan.Zero)
            {
                _logger.LogWarning("Token already expired, no need to blacklist");
                return;
            }
            
            await BlacklistTokenAsync(token, expiry, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blacklisting token");
        }
    }

    public async Task BlacklistTokenAsync(string token, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Attempted to blacklist empty or null token");
            return;
        }

        try
        {
            var jti = ExtractJtiFromToken(token);
            if (string.IsNullOrEmpty(jti))
            {
                _logger.LogWarning("Cannot blacklist token without JTI claim");
                return;
            }

            var key = GetBlacklistKey(jti);
            var blacklistEntry = new TokenBlacklistEntry
            {
                TokenId = jti,
                BlacklistedAt = DateTime.UtcNow,
                Reason = "Manual revocation",
            };
            
            await _cacheService.SetAsync(key, blacklistEntry, expiry, cancellationToken);
            _logger.LogInformation("Token {TokenId} blacklisted successfully", jti);
        } 
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blacklisting token with expiry");
        }
    }

    public async Task<bool> IsTokenBlacklistedAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        try
        {
            var jti = ExtractJtiFromToken(token);
            if (string.IsNullOrEmpty(jti))
                return false;

            var key = GetBlacklistKey(jti);
            var blacklistEntry = await _cacheService.GetAsync<TokenBlacklistEntry>(key, cancellationToken);
            
            var isBlacklisted = blacklistEntry != null;
            
            if (isBlacklisted)
            {
                _logger.LogDebug("Token {TokenId} found in blacklist", jti);
            }
            
            return isBlacklisted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking token blacklist status");
            // Trong trường hợp lỗi, return false để không block user unnecessarily
            return false;
        }
    }

    public async Task BlacklistAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userTokensKey = GetUserTokensKey(userId);
            var blacklistEntry = new UserTokenBlacklistEntry
            {
                UserId = userId,
                BlacklistedAt = DateTime.UtcNow,
                Reason = "All tokens revoked",
            };

            var expiry = TimeSpan.FromMinutes(_jwtSettings.AccessTokenExpirationInMinutes);
            await _cacheService.SetAsync(userTokensKey, blacklistEntry, expiry, cancellationToken);
            
            _logger.LogInformation("All tokens for user {UserId} blacklisted", userId);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error blacklisting all user tokens for user {UserId}", userId);
        }
    }

    public async Task CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        // Redis tự động cleanup expired keys, nhưng chúng ta có thể implement
        // additional cleanup logic ở đây nếu cần
        _logger.LogDebug("JWT blacklist cleanup completed");
        await Task.CompletedTask;
    }

    private string? ExtractJtiFromToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            if (!tokenHandler.CanReadToken(token)) return null;
            var jsonToken = tokenHandler.ReadJwtToken(token);
            return jsonToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
        }
        catch
        {
            return null;
        }
    }

    private TimeSpan GetTokensRemainingTime(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            if (!tokenHandler.CanReadToken(token)) return TimeSpan.Zero;
            var expiry = tokenHandler.ReadJwtToken(token).ValidTo;
            return expiry > DateTime.UtcNow ? expiry - DateTime.UtcNow : TimeSpan.Zero;
        }
        catch
        {
            return TimeSpan.Zero;
        }
    }

    private string GetBlacklistKey(string jti) => $"{_redisSettings.Cache.Jwt.BlacklistKeyPrefix}{jti}";
    private string GetUserTokensKey(Guid userId) => $"{_redisSettings.Cache.Jwt.UserTokensKeyPrefix}{userId}";
}

public class TokenBlacklistEntry
{
    public string TokenId { get; set; } = string.Empty;
    public DateTime BlacklistedAt { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class UserTokenBlacklistEntry
{
    public Guid UserId { get; set; }
    public DateTime BlacklistedAt { get; set; }
    public string Reason { get; set; } = string.Empty;
}