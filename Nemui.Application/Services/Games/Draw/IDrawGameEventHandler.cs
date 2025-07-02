using Nemui.Shared.DTOs.Games.Draw;

namespace Nemui.Application.Services.Games.Draw;

public interface IDrawGameEventHandler
{
    Task HandleRoundStartedAsync(RoundStartedEvent roundEvent);
    Task HandleRoundEndedAsync(RoundEndedEvent roundEvent);
}