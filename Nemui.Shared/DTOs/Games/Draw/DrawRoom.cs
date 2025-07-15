using MessagePack;

namespace Nemui.Shared.DTOs.Games.Draw;

[MessagePackObject]
public record DrawRoom
{
    [Key("roomId")]
    public Guid RoomId { get; set; }
    [Key("roomName")]
    public string RoomName { get; set; } = string.Empty;
    [Key("theme")]
    public string Theme { get; set; } = string.Empty;
    [Key("host")]
    public DrawHost Host { get; set; } = new();
    [Key("config")]
    public DrawRoomConfig Config { get; set; } = new();
};