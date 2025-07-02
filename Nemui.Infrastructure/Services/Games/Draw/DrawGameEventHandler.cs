using Microsoft.Extensions.Logging;
using Nemui.Application.Services.Games.Draw;
using Nemui.Shared.DTOs.Games.Draw;

namespace Nemui.Infrastructure.Services.Games.Draw;

public class DrawGameEventHandler(
    IDrawGameNotificationService notificationService,
    ILogger<DrawGameEventHandler> logger) : IDrawGameEventHandler
{
    public async Task HandleRoundStartedAsync(RoundStartedEvent roundEvent)
    {
        logger.LogInformation("Processing round started event for room {RoomId}", roundEvent.RoomId);
        await notificationService.NotifyRoundStartedAsync(roundEvent.RoomId, roundEvent);
    }

    public async Task HandleRoundEndedAsync(RoundEndedEvent roundEvent)
    {
        logger.LogInformation("Processing round ended event for room {RoomId}", roundEvent.RoomId);
        await notificationService.NotifyRoundEndedAsync(roundEvent.RoomId, roundEvent);
    }

}
