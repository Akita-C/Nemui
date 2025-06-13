namespace Nemui.Shared.DTOs.Quiz;

public class OrderingConfig
{
    public List<OrderItem> Items { get; set; } = new();
    public List<string> CorrectOrder { get; set; } = new();
    public bool ShowNumbers { get; set; } = true;
}

public class OrderItem
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}