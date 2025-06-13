namespace Nemui.Shared.DTOs.Quiz;

public class MultipleChoiceConfig
{
    public List<Option> Options { get; set; } = [];
    public List<int> CorrectAnswerIndices { get; set; } = new();
    public bool AllowMultipleSelection { get; set; } = false;
    public bool ShuffleOptions { get; set; } = true;
}

public class Option
{
    public string Text { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}