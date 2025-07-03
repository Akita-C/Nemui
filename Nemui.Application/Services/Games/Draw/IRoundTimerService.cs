using Nemui.Shared.DTOs.Games.Draw;
using Nemui.Shared.Enums;

namespace Nemui.Application.Services.Games.Draw;

public interface IRoundTimerService : IDisposable
{
    Task StartRoundAsync(Guid roomId, int roundNumber, int totalRounds, DrawRoomConfig config);
    Task StopRoundAsync(Guid roomId);
    Task<bool> IsPhaseActiveAsync(Guid roomId, DrawGamePhase phase);
    Task<bool> IsRoundActiveAsync(Guid roomId);
    Task<int?> GetRemainingTimeAsync(Guid roomId);
    Task<DrawGamePhase?> GetCurrentPhaseAsync(Guid roomId);

    event Func<RoundStartedEvent, Task>? OnRoundStarted;
    event Func<RoundEndedEvent, Task>? OnRoundEnded;
    event Func<PhaseChangedEvent, Task>? OnPhaseChanged;
}