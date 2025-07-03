using MessagePack;

namespace Nemui.Shared.DTOs.Games.Draw;

[MessagePackObject]
public record EndedGameEvent
{
    [Key("roomId")]
    public Guid RoomId { get; set; }
}