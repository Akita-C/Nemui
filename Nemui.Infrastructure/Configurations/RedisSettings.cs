namespace Nemui.Infrastructure.Configurations;

public class RedisSettings
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = string.Empty;
    public string? Host { get; set; }
    public int Port { get; set; } = 6379;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int Database { get; set; } = 0;
    public string InstanceName { get; set; } = "Akita";
    public bool UseSsl { get; set; } = false;

    public string GetConnectionString()
    {
        if (!string.IsNullOrEmpty(ConnectionString))
            return ConnectionString;

        var options = new List<string>();
        
        if (!string.IsNullOrEmpty(Host))
            options.Add($"{Host}:{Port}");
        
        if (!string.IsNullOrEmpty(Username))
            options.Add($"user={Username}");
            
        if (!string.IsNullOrEmpty(Password))
            options.Add($"password={Password}");
            
        if (UseSsl)
            options.Add("ssl=true");
            
        options.Add($"abortConnect=false");

        return string.Join(",", options);
    }
    
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