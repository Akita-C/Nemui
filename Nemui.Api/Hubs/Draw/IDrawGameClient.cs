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
    Task RoomStateUpdated(string state);
    Task DrawActionReceived(DrawAction action);
    // Task LiveDrawActionReceived(DrawAction action);
    Task RoundStarted(RoundStartedEvent roundEvent);
    Task RoundEnded(RoundEndedEvent roundEvent);
    Task PhaseChanged(PhaseChangedEvent phaseEvent);
}