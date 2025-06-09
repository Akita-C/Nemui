using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Nemui.Application.Services;

namespace Nemui.Infrastructure.Services;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(IDistributedCache distributedCache, ILogger<RedisCacheService> logger)
    {
        _distributedCache = distributedCache;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }
    
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var cachedValue = await _distributedCache.GetStringAsync(key, cancellationToken);
            if (string.IsNullOrEmpty(cachedValue))
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return null;
            }
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(cachedValue, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached value for key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var options = new DistributedCacheEntryOptions();
            if (expiry.HasValue)
            {
                options.SetAbsoluteExpiration(expiry.Value);
            }
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            await _distributedCache.SetStringAsync(key, serializedValue, options, cancellationToken);
            _logger.LogDebug("Cache set for key: {Key} with expiry: {Expiry}", key, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache value for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Cache removed for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache value for key: {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // This would require IConnectionMultiplexer for pattern-based deletion
        // For now, we'll implement basic pattern matching
        _logger.LogWarning("Pattern-based cache removal not implemented for key pattern: {Pattern}", pattern);
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _distributedCache.GetAsync(key, cancellationToken);
            return value != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache existence for key: {Key}", key);
            return false;
        }
    }
}