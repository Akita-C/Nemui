namespace Nemui.Infrastructure.Configurations;

public class RoundTimerSettings
{
    public const string SectionName = "RoundTimer";

    public TimeSpan CleanupIntervalMinutes { get; set; } = TimeSpan.FromMinutes(2);
    public TimeSpan FinishedRoundTimeoutMinutes { get; set; } = TimeSpan.FromMinutes(5);
}