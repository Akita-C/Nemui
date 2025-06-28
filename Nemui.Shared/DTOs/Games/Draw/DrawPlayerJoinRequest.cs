
using MessagePack;

namespace Nemui.Shared.DTOs.Games.Draw;

[MessagePackObject]
public record DrawPlayerJoinRequest(
    [property: Key("playerId")]
    string PlayerId, 
    [property: Key("playerName")]
    string PlayerName, 
    [property: Key("playerAvatar")]
    string? PlayerAvatar
);