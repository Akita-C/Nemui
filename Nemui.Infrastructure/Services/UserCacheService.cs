using Microsoft.Extensions.Options;
using Nemui.Application.Services;
using Nemui.Infrastructure.Configurations;
using Nemui.Shared.DTOs.Auth;

namespace Nemui.Infrastructure.Services;

public class UserCacheService : IUserCacheService
{
    private readonly ICacheService _cacheService;
    private readonly RedisSettings _redisSettings;
    
    public UserCacheService(ICacheService cacheService, IOptions<RedisSettings> redisSettings)
    {
        _cacheService = cacheService;
        _redisSettings = redisSettings.Value;
    }

    public async Task<UserProfileDto?> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var key = GetUserProfileKey(userId);
        return await _cacheService.GetAsync<UserProfileDto>(key, cancellationToken);
    }

    public async Task SetUserProfileAsync(Guid userId, UserProfileDto userProfile, CancellationToken cancellationToken = default)
    {
        var key = GetUserProfileKey(userId);
        var expiry = _redisSettings.Cache.User.ProfileTtl;
        await _cacheService.SetAsync(key, userProfile, expiry, cancellationToken);
    }

    public async Task InvalidateUserProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var key = GetUserProfileKey(userId);
        await _cacheService.RemoveAsync(key, cancellationToken);
    }

    public async Task InvalidateAllUserDataAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Invalidate profile cache
        await InvalidateUserProfileAsync(userId, cancellationToken);
        
        // In future: invalidate permissions, settings, etc.
        // var permissionsKey = GetUserPermissionsKey(userId);
        // await _cacheService.RemoveAsync(permissionsKey, cancellationToken);
    }

    private string GetUserProfileKey(Guid userId) => $"{_redisSettings.Cache.User.KeyPrefix}:profile:{userId}";
}