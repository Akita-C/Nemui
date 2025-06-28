using MessagePack;

namespace Nemui.Shared.DTOs.Games.Draw;

[MessagePackObject]
public record StrokeActionData
{
    [Key("path")]
    public string Path { get; set; } = string.Empty;
    [Key("color")]
    public string Color { get; set; } = string.Empty;
    [Key("width")]
    public int Width { get; set; }
    [Key("tool")]
    public string Tool { get; set; } = string.Empty; // "brush", "eraser
}