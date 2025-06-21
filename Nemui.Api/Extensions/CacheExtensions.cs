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
        if (redisSettings == null) throw new ArgumentNullException(nameof(redisSettings));
        
        services.AddStackExchangeRedisCache(options =>
        {
            options.InstanceName = redisSettings.InstanceName;
            options.ConfigurationOptions = new ConfigurationOptions
            {
                EndPoints = { { redisSettings.EndPoint, redisSettings.Port } },
                Password = redisSettings.Password,
                User = redisSettings.Username
            };
        });

        services.AddSingleton<IConnectionMultiplexer>(options =>
        {
            var connection = ConnectionMultiplexer.Connect(
                new ConfigurationOptions
                {
                    EndPoints = { { redisSettings.EndPoint, redisSettings.Port } },
                    Password = redisSettings.Password,
                    User = redisSettings.Username
                }
            );
            return connection;
        });

        services.AddScoped<IDatabase>(options =>
        {
            var connection = options.GetRequiredService<IConnectionMultiplexer>();
            return connection.GetDatabase();
        });
        
        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<IUserCacheService, UserCacheService>();
        
        return services;
    }
} 