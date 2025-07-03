using Nemui.Shared.DTOs.Games.Draw;

namespace Nemui.Application.Services.Games.Draw;

public interface IDrawGameNotificationService
{
    Task NotifyRoundStartedAsync(RoundStartedEvent roundEvent);
    Task NotifyRoundEndedAsync(RoundEndedEvent roundEvent);
    Task NotifyPhaseChangedAsync(PhaseChangedEvent phaseEvent);
}