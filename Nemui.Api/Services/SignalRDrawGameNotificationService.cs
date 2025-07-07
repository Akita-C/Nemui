using Microsoft.AspNetCore.SignalR;
using Nemui.Api.Hubs.Draw;
using Nemui.Application.Services.Games.Draw;
using Nemui.Shared.DTOs.Games.Draw;
using Nemui.Shared.Enums;

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
        try
        {
            var roomKey = gameService.GetRoomMetadataKey(roundStartEvent.RoomId);
            await hubContext.Clients.Group(roomKey).RoundStarted(roundStartEvent);
            logger.LogDebug("Current drawer id: {CurrentDrawerId}", roundStartEvent.CurrentDrawerId);
            await hubContext.Clients.User(roundStartEvent.CurrentDrawerId!).WordToDraw(roundStartEvent.CurrentWord!);
            logger.LogDebug("Round started notification sent to room {RoomId}", roundStartEvent.RoomId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending round started notification to room {RoomId}", roundStartEvent.RoomId);
        }
    }

    public async Task NotifyRoundEndedAsync(EndedGameEvent endedGameEvent)
    {
        try
        {
            var roomKey = gameService.GetRoomMetadataKey(endedGameEvent.RoomId);
            await hubContext.Clients.Group(roomKey).EndedGame(endedGameEvent);
            logger.LogDebug("Round ended notification sent to room {RoomId}", endedGameEvent.RoomId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending round ended notification to room {RoomId}", endedGameEvent.RoomId);
        }
    }

    public async Task NotifyPhaseChangedAsync(PhaseChangedEvent phaseEvent)
    {
        try
        {
            var roomKey = gameService.GetRoomMetadataKey(phaseEvent.RoomId);
            if (phaseEvent.Phase == DrawGamePhase.Drawing)
            {
                await hubContext.Clients.User(phaseEvent.CurrentDrawerId!).WordToDraw(phaseEvent.CurrentWord!);
                phaseEvent = phaseEvent with { CurrentWord = null };
            }
            await hubContext.Clients.GroupExcept(roomKey, phaseEvent.CurrentDrawerId!).PhaseChanged(phaseEvent);
            logger.LogDebug("Phase changed to {Phase} notification sent to room {RoomId}", phaseEvent.Phase, phaseEvent.RoomId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending phase changed notification to room {RoomId}", phaseEvent.RoomId);
        }
    }

    public async Task NotifyWordRevealedAsync(WordRevealedEvent wordRevealedEvent)
    {
        try
        {
            var roomKey = gameService.GetRoomMetadataKey(wordRevealedEvent.RoomId);
            var currentDrawer = await gameService.GetCurrentDrawerAsync(wordRevealedEvent.RoomId);
            if (currentDrawer == null)
            {
                logger.LogError("Current drawer not found for room {RoomId}", wordRevealedEvent.RoomId);
                return;
            }
            var player = await gameService.GetPlayerAsync(currentDrawer!, wordRevealedEvent.RoomId);
            if (player == null)
            {
                logger.LogError("Cannot find player for room {RoomId} and with id {PlayerId}", wordRevealedEvent.RoomId, currentDrawer);
                return;
            }
            await hubContext.Clients.GroupExcept(roomKey, player.ConnectionId!).WordRevealed(wordRevealedEvent);
            logger.LogDebug("Word revealed notification sent to room {RoomId} to all players except {PlayerId}", wordRevealedEvent.RoomId, player.PlayerId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending word revealed notification to room {RoomId}", wordRevealedEvent.RoomId);
        }
    }
}