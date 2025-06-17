using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Nemui.Application.Common.Interfaces;
using Nemui.Application.Services.Games.Draw;

namespace Nemui.Api.Hubs.Draw;

[Authorize]
public class DrawGameHub(
    IDrawGameService gameService, 
    ILogger<DrawGameHub> logger,
    ICurrentUserService currentUserService
) : Hub<IDrawGameClient>
{
    public async Task JoinRoom(Guid roomId)
    {
        var room = await gameService.GetRoomAsync(roomId);
        if (room is null || room.Config is null)
        {
            logger.LogWarning("Attempted to join non-existent room {RoomId}", roomId);
            throw new HubException("Room not found or configuration is missing.");
        }
        await Groups.AddToGroupAsync(Context.ConnectionId, gameService.GetRoomName(roomId));
        await Clients.GroupExcept(gameService.GetRoomName(roomId), Context.ConnectionId)
            .UserJoined(currentUserService.Name ?? "Unknown User");
        await Clients.Caller.JoinRoom(room);
    }

    public async Task LeaveRoom(Guid roomId)
    {
        var room = await gameService.GetRoomAsync(roomId);
        if (room is null)
        {
            logger.LogWarning("Attempted to leave non-existent room {RoomId}", roomId);
            throw new HubException("Room not found.");
        }

        // If the current user is not the host, notify others and remove from group
        if (room.DrawHost.HostId != currentUserService.UserId)
        {
            await Clients.GroupExcept(gameService.GetRoomName(roomId), Context.ConnectionId)
                .UserLeft(currentUserService.Name ?? "Unknown User");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameService.GetRoomName(roomId));
            await Clients.Caller.LeaveRoom();   
        }
        // If the current user is the host
        else
        {
            
        }
    }

    public async Task SendRoomMessage(Guid roomId ,string message)
    {
        await Clients.GroupExcept(gameService.GetRoomName(roomId), Context.ConnectionId)
            .RoomMessageReceived(Guid.Parse(currentUserService.UserId!), currentUserService.Name ?? "Unknown User", message);
    }
}