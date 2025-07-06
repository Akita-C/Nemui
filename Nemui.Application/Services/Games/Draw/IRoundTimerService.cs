using Nemui.Shared.DTOs.Games.Draw;

namespace Nemui.Application.Services.Games.Draw;

public interface IRoundTimerService : IDisposable
{
    Task StartRoundAsync(Guid roomId, int totalRounds, DrawRoomConfig config);
    Task StopRoundAsync(Guid roomId);
    Task<bool> IsRoundActiveAsync(Guid roomId);
    Task<int?> GetRemainingTimeAsync(Guid roomId);

    event Func<RoundStartedEvent, Task>? OnRoundStarted;
    event Func<EndedGameEvent, Task>? OnEndedGame;
    event Func<PhaseChangedEvent, Task>? OnPhaseChanged;
    event Func<WordRevealedEvent, Task>? OnWordRevealed;
}