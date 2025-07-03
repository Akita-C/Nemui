using Nemui.Shared.DTOs.Games.Draw;

namespace Nemui.Application.Services.Games.Draw;

public interface IDrawGameNotificationService
{
    Task NotifyRoundStartedAsync(RoundStartedEvent roundEvent);
    Task NotifyRoundEndedAsync(EndedGameEvent roundEvent);
    Task NotifyPhaseChangedAsync(PhaseChangedEvent phaseEvent);
}