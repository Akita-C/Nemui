namespace Nemui.Shared.DTOs.Quiz;

public class MatchingConfig
{
    public List<MatchItem> LeftItems { get; set; } = [];
    public List<MatchItem> RightItems { get; set; } = [];
    public List<MatchPair> CorrectMatches { get; set; } = [];
    public bool ShuffleItems { get; set; } = true;
}

public class MatchItem
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}

public class MatchPair
{
    public string LeftId { get; set; } = string.Empty;
    public string RightId { get; set; } = string.Empty;
}