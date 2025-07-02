using Nemui.Shared.DTOs.Games.Draw;

namespace Nemui.Application.Services.Games.Draw;

public interface IDrawGameNotificationService
{
    Task NotifyRoundStartedAsync(Guid roomId, RoundStartedEvent roundEvent);
    Task NotifyRoundEndedAsync(Guid roomId, RoundEndedEvent roundEvent);
}