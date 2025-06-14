namespace Nemui.Infrastructure.Configurations;

public class AdminSettings
{
    public const string SectionName = "AdminSettings";
    
    public string SeedApiKey { get; set; } = string.Empty;
    public bool EnableSeedEndpoints { get; set; } = false;
} 