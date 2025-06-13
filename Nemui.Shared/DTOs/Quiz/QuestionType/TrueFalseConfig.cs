namespace Nemui.Shared.DTOs.Quiz;

public class TrueFalseConfig
{
    public bool CorrectAnswer { get; set; }
    public string? TrueLabel { get; set; } = "True";
    public string? FalseLabel { get; set; } = "False";
}