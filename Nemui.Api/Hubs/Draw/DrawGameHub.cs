using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Nemui.Application.Common.Interfaces;
using Nemui.Application.Services.Games.Draw;
using Nemui.Shared.DTOs.Games.Draw;

namespace Nemui.Api.Hubs.Draw;

[Authorize]
public class DrawGameHub(
    IDrawGameService gameService,
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
        var room = await gameService.GetRoomAsync(roomId);
        await Groups.AddToGroupAsync(Context.ConnectionId, gameService.GetRoomKey(roomId));
        var drawPlayer = new DrawPlayer(Context.ConnectionId, request.PlayerId, request.PlayerName, request.PlayerAvatar);
        await gameService.AddPlayerAsync(roomId, drawPlayer);
        await Clients.GroupExcept(gameService.GetRoomKey(roomId), Context.ConnectionId).UserJoined(drawPlayer);
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
            await Clients.GroupExcept(gameService.GetRoomKey(roomId), player.ConnectionId!).UserLeft(player);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameService.GetRoomKey(roomId));
            await gameService.RemovePlayerAsync(roomId, player);
            await Clients.Caller.LeaveRoom();
        }
        // If the current user is the host
        else
        {
            await Clients.Group(gameService.GetRoomKey(roomId)).RoomDeleted();
            await gameService.DeleteRoomAsync(roomId);
        }
        logger.LogInformation("User {UserId} left room {RoomId}", player.PlayerId, roomId);
    }

    public async Task SendRoomMessage(Guid roomId, string message)
    {
        await Clients.GroupExcept(gameService.GetRoomKey(roomId), Context.ConnectionId)
            .RoomMessageReceived(Guid.Parse(currentUserService.UserId!), currentUserService.Name ?? "Unknown User", message);
    }

    public async Task KickPlayer(Guid roomId, DrawPlayer player)
    {
        await gameService.RemovePlayerAsync(roomId, player);
        await Groups.RemoveFromGroupAsync(player.ConnectionId!, gameService.GetRoomKey(roomId));
        await Clients.GroupExcept(gameService.GetRoomKey(roomId), player.ConnectionId!).UserLeft(player);
    }
}