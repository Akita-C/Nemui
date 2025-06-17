namespace Nemui.Shared.DTOs.Games.Draw;

public record DrawRoomConfig
{
    public int MaxPlayers { get; set; }
    public int MaxRoundPerPlayers { get; set; }
};