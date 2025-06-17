using Nemui.Shared.DTOs.Games.Draw;

namespace Nemui.Api.Hubs.Draw;

public interface IDrawGameClient
{
    Task JoinRoom(DrawRoom room);
    Task LeaveRoom();
    Task UserJoined(string userName);
    Task UserLeft(string userName);
    Task RoomMessageReceived(Guid senderId, string senderName, string message);
}