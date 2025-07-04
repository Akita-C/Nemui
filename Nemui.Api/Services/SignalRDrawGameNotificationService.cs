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

    public async Task NotifyRoundStartedAsync(RoundStartedEvent roundStartEvent)
    {
        var roomKey = gameService.GetRoomMetadataKey(roundStartEvent.RoomId);
        await hubContext.Clients.Group(roomKey).RoundStarted(roundStartEvent);
        logger.LogDebug("Round started notification sent to room {RoomId}", roundStartEvent.RoomId);
    }

    public async Task NotifyRoundEndedAsync(EndedGameEvent endedGameEvent)
    {
        var roomKey = gameService.GetRoomMetadataKey(endedGameEvent.RoomId);
        await hubContext.Clients.Group(roomKey).EndedGame(endedGameEvent);
        logger.LogDebug("Round ended notification sent to room {RoomId}", endedGameEvent.RoomId);
    }

    public async Task NotifyPhaseChangedAsync(PhaseChangedEvent phaseEvent)
    {
        var roomKey = gameService.GetRoomMetadataKey(phaseEvent.RoomId);
        await hubContext.Clients.Group(roomKey).PhaseChanged(phaseEvent);
        logger.LogDebug("Phase changed to {Phase} notification sent to room {RoomId}", phaseEvent.Phase, phaseEvent.RoomId);
    }
}