using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Nemui.Shared.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DrawGamePhase
{
    [EnumMember(Value = "waiting")]
    Waiting,
    [EnumMember(Value = "drawing")]
    Drawing,
    [EnumMember(Value = "guessing")]
    Guessing,
    [EnumMember(Value = "reveal")]
    Reveal,
    [EnumMember(Value = "finished")]
    Finished
}