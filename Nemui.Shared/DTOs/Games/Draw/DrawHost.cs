
using MessagePack;

namespace Nemui.Shared.DTOs.Games.Draw;

[MessagePackObject]
public record DrawHost
{
    [Key("hostId")]
    public string HostId { get; set; } = string.Empty;
    [Key("hostName")]
    public string HostName { get; set; } = string.Empty;
};