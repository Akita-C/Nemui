
using MessagePack;

namespace Nemui.Shared.DTOs.Games.Draw;

[MessagePackObject]
public record DrawPlayer(
    [property: Key("connectionId")]
    string? ConnectionId, 
    [property: Key("playerId")]
    string PlayerId, 
    [property: Key("playerName")]
    string PlayerName, 
    [property: Key("playerAvatar")]
    string? PlayerAvatar
);