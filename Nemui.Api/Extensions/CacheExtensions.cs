using StackExchange.Redis;
using Nemui.Infrastructure.Services;
using Nemui.Infrastructure.Configurations;
using Nemui.Application.Services;

namespace Nemui.Api.Extensions;

public static class CacheExtensions
{
    public static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
    {
        var redisSettings = configuration.GetSection(RedisSettings.SectionName).Get<RedisSettings>();
        
        services.AddStackExchangeRedisCache(options =>
        {
            if (redisSettings == null) throw new ArgumentNullException(nameof(redisSettings));
            options.InstanceName = redisSettings.InstanceName;
            options.ConfigurationOptions = new ConfigurationOptions
            {
                EndPoints = { { redisSettings.EndPoint, redisSettings.Port } },
                Password = redisSettings.Password,
                User = redisSettings.Username
            };
        });
        
        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<IUserCacheService, UserCacheService>();
        
        return services;
    }
} 