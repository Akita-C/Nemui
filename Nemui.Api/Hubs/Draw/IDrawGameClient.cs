using Nemui.Shared.DTOs.Games.Draw;

namespace Nemui.Api.Hubs.Draw;

public interface IDrawGameClient
{
    Task JoinRoom(DrawPlayer player);
    Task LeaveRoom();
    Task UserJoined(DrawPlayer player);
    Task UserLeft(DrawPlayer player);
    Task RoomMessageReceived(Guid senderId, string senderName, string message);
    Task RoomDeleted();
}