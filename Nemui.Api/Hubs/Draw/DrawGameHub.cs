using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Nemui.Application.Common.Interfaces;
using Nemui.Application.Services.Games.Draw;
using Nemui.Shared.DTOs.Games.Draw;
using Nemui.Shared.Enums;

namespace Nemui.Api.Hubs.Draw;

[Authorize]
public class DrawGameHub(
    IDrawGameService gameService,
    IRoundTimerService roundTimerService,
    ILogger<DrawGameHub> logger,
    ICurrentUserService currentUserService
) : Hub<IDrawGameClient>
{
    public async Task JoinRoom(Guid roomId, DrawPlayerJoinRequest request)
    {
        var isRoomExists = await gameService.IsRoomExistsAsync(roomId);
        var isRoomFull = await gameService.IsRoomFullAsync(roomId);
        if (!isRoomExists || isRoomFull)
        {
            logger.LogWarning("Attempted to join non-existent room {RoomId}", roomId);
            throw new HubException("Room not found or configuration is missing.");
        }
        logger.LogInformation("User {UserId} joined room {RoomId}", request.PlayerId, roomId);
        await Groups.AddToGroupAsync(Context.ConnectionId, gameService.GetRoomMetadataKey(roomId));
        var drawPlayer = new DrawPlayer(Context.ConnectionId, request.PlayerId, request.PlayerName, request.PlayerAvatar);
        await gameService.AddPlayerAsync(roomId, drawPlayer);
        await Clients.GroupExcept(gameService.GetRoomMetadataKey(roomId), Context.ConnectionId).UserJoined(drawPlayer);
        await Clients.Caller.JoinRoom(drawPlayer);
    }

    public async Task LeaveRoom(Guid roomId, DrawPlayer player)
    {
        var isRoomExists = await gameService.IsRoomExistsAsync(roomId);
        if (!isRoomExists)
        {
            logger.LogWarning("Attempted to leave non-existent room {RoomId}", roomId);
            throw new HubException("Room not found.");
        }
        var room = await gameService.GetRoomAsync(roomId);
        // If the current user is not the host, notify others and remove from group
        if (room!.Host.HostId != currentUserService.UserId)
        {
            await Clients.GroupExcept(gameService.GetRoomMetadataKey(roomId), player.ConnectionId!).UserLeft(player);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameService.GetRoomMetadataKey(roomId));
            await gameService.RemovePlayerAsync(roomId, player);
            await Clients.Caller.LeaveRoom();
        }
        // If the current user is the host
        else
        {
            await Clients.Group(gameService.GetRoomMetadataKey(roomId)).RoomDeleted();
            await gameService.DeleteRoomAsync(roomId);
        }
        logger.LogInformation("User {UserId} left room {RoomId}", player.PlayerId, roomId);
    }

    public async Task SendRoomMessage(Guid roomId, string message)
    {
        await Clients.GroupExcept(gameService.GetRoomMetadataKey(roomId), Context.ConnectionId)
            .RoomMessageReceived(Guid.Parse(currentUserService.UserId!), currentUserService.Name ?? "Unknown User", message);
    }

    public async Task KickPlayer(Guid roomId, DrawPlayer player)
    {
        await gameService.RemovePlayerAsync(roomId, player);
        await Groups.RemoveFromGroupAsync(player.ConnectionId!, gameService.GetRoomMetadataKey(roomId));
        await Clients.GroupExcept(gameService.GetRoomMetadataKey(roomId), player.ConnectionId!).UserLeft(player);
    }

    public async Task SendDrawAction(Guid roomId, DrawAction action)
    {
        var isDrawingPhase = await gameService.GetCurrentPhaseAsync(roomId) == DrawGamePhase.Drawing && currentUserService.UserId == await gameService.GetCurrentDrawerAsync(roomId);
        if (!isDrawingPhase)
        {
            logger.LogWarning("Invalid draw action attempt for room {RoomId} by user {UserId} because drawing phase is not active", roomId, currentUserService.UserId);
            throw new HubException("Drawing phase is not active.");
        }

        var isRoomExists = await gameService.IsRoomExistsAsync(roomId);
        var (isPlayerInRoom, _) = await gameService.IsPlayerInRoomAsync(currentUserService.UserId!, roomId);
        if (!isRoomExists || !isPlayerInRoom)
        {
            logger.LogWarning("Invalid draw action attempt for room {RoomId} by user {UserId}", roomId, currentUserService.UserId);
            throw new HubException("Room not found or user is not in room.");
        }
        await Clients.GroupExcept(gameService.GetRoomMetadataKey(roomId), Context.ConnectionId).DrawActionReceived(action);
    }

    // public async Task SendLiveDrawAction(Guid roomId, DrawAction action) 
    // {
    //     var isRoomExists = await gameService.IsRoomExistsAsync(roomId);
    //     var (isPlayerInRoom, _) = await gameService.IsPlayerInRoomAsync(currentUserService.UserId!, roomId);
    //     if (!isRoomExists || !isPlayerInRoom)
    //     {
    //         logger.LogWarning("Invalid live draw action attempt for room {RoomId} by user {UserId}", roomId, currentUserService.UserId);
    //         throw new HubException("Room not found or user is not in room.");
    //     }
    //     await Clients.GroupExcept(gameService.GetRoomKey(roomId), Context.ConnectionId).LiveDrawActionReceived(action);
    // }

    public async Task StartRound(Guid roomId)
    {
        var room = await gameService.GetRoomAsync(roomId);
        if (room == null || room.Host.HostId != currentUserService.UserId)
        {
            throw new HubException("Room not found or user is not the host.");
        }

        var totalRounds = room.Config.MaxRoundPerPlayers * (await gameService.GetPlayerCountAsync(roomId));
        var playerIds = (await gameService.GetAllPlayersAsync(currentUserService!.UserId, roomId)).Select(player => player!.PlayerId).ToList();
        await gameService.InitializeWordPoolAsync(roomId, (int)totalRounds);
        await gameService.InitializeGameSessionAsync(roomId, playerIds, (int)totalRounds);
        await gameService.InitializePlayerHeartsAsync(roomId, playerIds);
        await roundTimerService.StartRoundAsync(roomId, (int)totalRounds, room.Config);
    }

    public async Task SendGuessMessage(Guid roomId, string message)
    {
        var (isPlayerInRoom, _) = await gameService.IsPlayerInRoomAsync(currentUserService.UserId!, roomId);
        if (!isPlayerInRoom)
        {
            throw new HubException("User is not in room.");
        }
        var gamePhase = await gameService.GetCurrentPhaseAsync(roomId);
        var playerHearts = await gameService.GetPlayerHeartsAsync(roomId, currentUserService.UserId!);
        if (gamePhase != DrawGamePhase.Guessing && gamePhase != DrawGamePhase.Drawing || playerHearts <= 0)
        {
            if (gamePhase != DrawGamePhase.Guessing && gamePhase != DrawGamePhase.Drawing)
            {
                throw new HubException($"Invalid guess message attempt for room {roomId} by user {currentUserService.UserId} because game phase is not guessing or drawing");
            }
            if (playerHearts <= 0)
            {
                throw new HubException("User has no hearts left.");
            }
        }

        var (isCorrect, newScore) = await gameService.GuessWordAsync(roomId, currentUserService.UserId!, message);
        if (isCorrect)
            await Clients.Group(gameService.GetRoomMetadataKey(roomId)).GuessMessageCorrectReceived(currentUserService.UserId!, newScore);
        else
        {
            await Task.WhenAll([
                Clients.Group(gameService.GetRoomMetadataKey(roomId)).GuessMessageWrongReceived(currentUserService.UserId!, message),
                gameService.DecrementPlayerHeartsAsync(roomId, currentUserService.UserId!)
            ]);
        }
    }
}