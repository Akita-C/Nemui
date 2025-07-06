using MessagePack;

namespace Nemui.Shared.DTOs.Games.Draw;

[MessagePackObject]
public record DrawRoomConfig
{
    [Key("maxPlayers")]
    public int MaxPlayers { get; set; }

    [Key("maxRoundPerPlayers")]
    public int MaxRoundPerPlayers { get; set; }

    // ============== Game duration config ==============

    [Key("drawingDurationSeconds")]
    public int DrawingDurationSeconds { get; set; } = 20;

    [Key("guessingDurationSeconds")]
    public int GuessingDurationSeconds { get; set; } = 10;

    [Key("revealDurationSeconds")]
    public int RevealDurationSeconds { get; set; } = 5;

    // ============== Word reveal config ==============

    [Key("wordRevealIntervalSeconds")]
    public int WordRevealIntervalSeconds { get; set; } = 5;

    [Key("maxWordRevealPercentage")]
    public float MaxWordRevealPercentage { get; set; } = 0.6f;

    [Key("enableWordReveal")]
    public bool EnableWordReveal { get; set; } = true;
};