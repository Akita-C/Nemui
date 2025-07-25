using MessagePack;
using Nemui.Shared.Enums;

namespace Nemui.Shared.DTOs.Games.Draw;

[MessagePackObject]
public record PhaseChangedEvent
{
    [Key("roomId")]
    public Guid RoomId { get; set; }
    [Key("roundNumber")]
    public int RoundNumber { get; set; }
    [Key("phase")]
    public DrawGamePhase Phase { get; set; }
    [Key("durationSeconds")]
    public int DurationSeconds { get; set; }
    [Key("startTime")]
    public DateTimeOffset StartTime { get; set; }
    [Key("currentDrawerId")]
    public string? CurrentDrawerId { get; set; }
    [Key("currentWord")]
    public string? CurrentWord { get; set; }
}