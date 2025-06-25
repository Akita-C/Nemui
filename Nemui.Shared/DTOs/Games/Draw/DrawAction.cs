namespace Nemui.Shared.DTOs.Games.Draw;

public record DrawAction 
{
    public string Id { get; init; } = string.Empty;
    public DrawActionType Type { get; init; }
    public long Timestamp { get; init; }
    public object Data { get; init; } = new();
}

public enum DrawActionType
{
    Stroke,
    Shape,
    Clear,
    Undo,
    Redo
}