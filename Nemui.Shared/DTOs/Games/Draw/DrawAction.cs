using System.Text.Json.Serialization;
using MessagePack;

namespace Nemui.Shared.DTOs.Games.Draw;

[MessagePackObject]
public record DrawAction
{
    [Key("id")]
    public string Id { get; set; } = string.Empty;
    [Key("type")]
    public DrawActionType Type { get; set; }
    [Key("timestamp")]
    public long Timestamp { get; set; }
    [Key("data")]
    public object Data { get; set; } = new();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DrawActionType
{
    Stroke,
    Shape,
    Clear,
    Undo,
    Redo,
    LiveStrokeStart,
    LiveStrokeMove,
    LiveStrokeEnd,
    LiveShapeStart,
    LiveShapeMove,
    LiveShapeEnd,
}