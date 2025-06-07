namespace Nemui.Infrastructure.Configurations;

public class AuthSettings
{
    public const string SectionName = "AuthSettings";
    
    public int MaxFailedLoginAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 30;
    public int ExpiredTokenCleanupDays { get; set; } = 30;
    public bool RequireEmailConfirmation { get; set; } = false;
    public PasswordRequirements PasswordRequirements { get; set; } = new();
}

public class PasswordRequirements
{
    public int MinLength { get; set; } = 8;
    public int MaxLength { get; set; } = 100;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
    public bool RequireSpecialChar { get; set; } = true;
}