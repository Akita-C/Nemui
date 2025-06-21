namespace Nemui.Shared.DTOs.Games.Draw;

public record DrawRoom
{
    public Guid RoomId { get; init; }
    public string RoomName { get; init; } = string.Empty;
    public DrawHost Host { get; init; } = new();
    public DrawRoomConfig Config { get; init; } = new();
};