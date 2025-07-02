using Nemui.Shared.DTOs.Games.Draw;

namespace Nemui.Application.Services.Games.Draw;

public interface IRoundTimerService : IDisposable
{
    Task StartRoundAsync(Guid roomId, int roundNumber, int totalRounds, int durationSeconds);
    Task StopRoundAsync(Guid roomId);
    Task<bool> IsRoundActiveAsync(Guid roomId);
    Task<int?> GetRemainingTimeAsync(Guid roomId);

    event Func<RoundStartedEvent, Task>? OnRoundStarted;
    event Func<RoundEndedEvent, Task>? OnRoundEnded;
}