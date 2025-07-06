using MessagePack;

namespace Nemui.Shared.DTOs.Games.Draw;

[MessagePackObject]
public record WordRevealedEvent
{
    [Key("roomId")]
    public Guid RoomId { get; set; }

    [Key("revealedWord")]
    public string RevealedWord { get; set; } = string.Empty;

    [Key("revealPercentage")]
    public float RevealPercentage { get; set; }

    [Key("timestamp")]
    public DateTimeOffset Timestamp { get; set; }
};