namespace Nemui.Shared.DTOs.Games.Draw;

public record UndoRedoActionData
{
    public string TargetActionId { get; init; } = string.Empty;
}