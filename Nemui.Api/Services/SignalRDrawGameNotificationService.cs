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

    public async Task NotifyRoundStartedAsync(RoundStartedEvent roundEvent)
    {
        var roomKey = gameService.GetRoomKey(roundEvent.RoomId);
        await hubContext.Clients.Group(roomKey).RoundStarted(roundEvent);
        logger.LogDebug("Round started notification sent to room {RoomId}", roundEvent.RoomId);
    }

    public async Task NotifyRoundEndedAsync(RoundEndedEvent roundEvent)
    {
        var roomKey = gameService.GetRoomKey(roundEvent.RoomId);
        await hubContext.Clients.Group(roomKey).RoundEnded(roundEvent);
        logger.LogDebug("Round ended notification sent to room {RoomId}", roundEvent.RoomId);
    }

    public async Task NotifyPhaseChangedAsync(PhaseChangedEvent phaseEvent)
    {
        var roomKey = gameService.GetRoomKey(phaseEvent.RoomId);
        await hubContext.Clients.Group(roomKey).PhaseChanged(phaseEvent);
        logger.LogDebug("Phase changed to {Phase} notification sent to room {RoomId}", phaseEvent.Phase, phaseEvent.RoomId);
    }
}