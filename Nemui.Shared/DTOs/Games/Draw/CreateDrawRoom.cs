namespace Nemui.Shared.DTOs.Games.Draw;

public record CreateDrawRoom
{
    public string RoomName { get; set; } = string.Empty;
    public DrawRoomConfig Config { get; set; } = new();
}