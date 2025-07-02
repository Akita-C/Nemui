using MessagePack;

namespace Nemui.Shared.DTOs.Games.Draw;

[MessagePackObject]
public record RoundEndedEvent
{
    [Key("roomId")]
    public Guid RoomId { get; set; }
    [Key("roundNumber")]
    public int RoundNumber { get; set; }
    [Key("isGameFinished")]
    public bool IsGameFinished { get; set; }
}