using MessagePack;

namespace Nemui.Shared.DTOs.Games.Draw;

[MessagePackObject]
public record DrawRoomConfig
{
    [Key("maxPlayers")]
    public int MaxPlayers { get; set; }

    [Key("maxRoundPerPlayers")]
    public int MaxRoundPerPlayers { get; set; }

    [Key("drawingDurationSeconds")]
    public int DrawingDurationSeconds { get; set; } = 80;

    [Key("guessingDurationSeconds")]
    public int GuessingDurationSeconds { get; set; } = 80;

    [Key("revealDurationSeconds")]
    public int RevealDurationSeconds { get; set; } = 80;
};