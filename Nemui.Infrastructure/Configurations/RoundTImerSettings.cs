namespace Nemui.Infrastructure.Configurations;

public class RoundTimerSettings
{
    public const string SectionName = "RoundTimer";

    public int CleanupIntervalMinutes { get; set; } = 2;
    public int FinishedRoundTimeoutMinutes { get; set; } = 5;

    public TimeSpan CleanupInterval => TimeSpan.FromMinutes(CleanupIntervalMinutes);
    public TimeSpan FinishedRoundTimeout => TimeSpan.FromMinutes(FinishedRoundTimeoutMinutes);
}