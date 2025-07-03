using MessagePack;
using Nemui.Shared.Enums;

namespace Nemui.Shared.DTOs.Games.Draw;

[MessagePackObject]
public record DrawGameSession
{
    [Key("roomId")]
    public Guid RoomId { get; set; }
    [Key("currentRound")]
    public int CurrentRound { get; set; }
    [Key("totalRounds")]
    public int TotalRounds { get; set; }
    [Key("currentDrawerId")]
    public string? CurrentDrawerId { get; set; }
    [Key("currentWord")]
    public string? CurrentWord { get; set; }
    [Key("playerTurnOrder")]
    public List<string> PlayerTurnOrder { get; set; } = [];
    [Key("currentTurnIndex")]
    public int CurrentTurnIndex { get; set; }
    [Key("phase")]
    public DrawGamePhase Phase { get; set; }
    [Key("sessionStartTime")]
    public DateTime SessionStartTime { get; set; }
    [Key("roundStartTime")]
    public DateTime RoundStartTime { get; set; }
}