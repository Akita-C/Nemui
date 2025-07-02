using Microsoft.AspNetCore.SignalR;
using Nemui.Api.Hubs.Draw;
using Nemui.Application.Services.Games.Draw;
using Nemui.Shared.DTOs.Games.Draw;

namespace Nemui.Api.Services;

public class SignalRDrawGameNotificationService : IDrawGameNotificationService
{
    private readonly IHubContext<DrawGameHub, IDrawGameClient> hubContext;
    private readonly IDrawGameService gameService;
    private readonly ILogger<SignalRDrawGameNotificationService> logger;

    public SignalRDrawGameNotificationService(
        IHubContext<DrawGameHub, IDrawGameClient> hubContext,
        IDrawGameService gameService,
        ILogger<SignalRDrawGameNotificationService> logger)
    {
        this.hubContext = hubContext;
        this.gameService = gameService;
        this.logger = logger;
    }

    public async Task NotifyRoundStartedAsync(Guid roomId, RoundStartedEvent roundEvent)
    {
        try
        {
            var roomKey = gameService.GetRoomKey(roomId);
            await hubContext.Clients.Group(roomKey).RoundStarted(roundEvent);
            logger.LogDebug("Round started notification sent to room {RoomId}", roomId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send round started notification to room {RoomId}", roomId);
            throw;
        }
    }

    public async Task NotifyRoundEndedAsync(Guid roomId, RoundEndedEvent roundEvent)
    {
        try
        {
            var roomKey = gameService.GetRoomKey(roomId);
            await hubContext.Clients.Group(roomKey).RoundEnded(roundEvent);
            logger.LogDebug("Round ended notification sent to room {RoomId}", roomId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send round ended notification to room {RoomId}", roomId);
            throw;
        }
    }
}