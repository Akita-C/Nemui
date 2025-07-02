using MessagePack;

namespace Nemui.Shared.DTOs.Games.Draw;

[MessagePackObject]
public record DrawRoomConfig
{
    [Key("maxPlayers")]
    public int MaxPlayers { get; set; }
    [Key("maxRoundPerPlayers")]
    public int MaxRoundPerPlayers { get; set; }
    [Key("roundDurationSeconds")]
    public int RoundDurationSeconds { get; set; } = 80;
};