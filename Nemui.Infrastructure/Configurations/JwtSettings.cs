using System.ComponentModel.DataAnnotations;

namespace Nemui.Infrastructure.Configurations;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";
    
    [Required]
    public string SecretKey { get; set; } = string.Empty;
    [Required]
    public string Issuer { get; set; } = string.Empty;
    [Required]
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpirationInMinutes { get; set; } = 15;
    public int RefreshTokenExpirationInDays { get; set; } = 30;
}