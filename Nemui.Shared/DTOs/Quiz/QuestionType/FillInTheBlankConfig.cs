namespace Nemui.Shared.DTOs.Quiz;

public class FillInTheBlankConfig
{
    public string TextWithBlanks { get; set; } = string.Empty;
    public List<BlankAnswer> Blanks { get; set; } = [];
    public bool CaseSensitive { get; set; } = false;
    public bool TrimWhitespace { get; set; } = true;
}

public class BlankAnswer
{
    public List<string> AcceptedAnswers { get; set; } = new();
    public int Position { get; set; }
}