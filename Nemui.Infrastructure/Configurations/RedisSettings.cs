namespace Nemui.Infrastructure.Configurations;

public class RedisSettings
{
    public const string SectionName = "Redis";

    public string EndPoint { get; set; } = string.Empty;
    public int Port { get; set; }
    public string? Username { get; init; }
    public string? Password { get; init; } 
    public string InstanceName { get; set; } = "Akita";
    
    public CacheSettings Cache { get; set; } = new();
}

public class CacheSettings
{
    public UserCacheSettings User { get; set; } = new();
    public JwtCacheSettings Jwt { get; set; } = new();
}

public class UserCacheSettings
{
    public TimeSpan ProfileTtl { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan PermissionTtl { get; set; } = TimeSpan.FromMinutes(30);
    public string KeyPrefix { get; set; } = "user";
}

public class JwtCacheSettings
{
    public string BlacklistKeyPrefix { get; set; } = "jwt:blacklist:";
    public string UserTokensKeyPrefix { get; set; } = "jwt:user:";
    public int CleanupIntervalMinutes { get; set; } = 60;
    public TimeSpan DefaultBlacklistTtl { get; set; } = TimeSpan.FromHours(24);
}