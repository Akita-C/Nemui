namespace Nemui.Shared.DTOs.Games.Draw;

public record StrokeActionData
{
    public string Path { get; init; } = string.Empty;
    public string Color { get; init; } = string.Empty;
    public int Width { get; init; }
    public string Tool { get; init; } = string.Empty; // "brush", "eraser
}