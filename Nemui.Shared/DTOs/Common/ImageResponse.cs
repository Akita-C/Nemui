namespace Nemui.Shared.DTOs.Common;

public class ImageResponse
{
    public string PublicId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string SecureUrl { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public string Format { get; set; } = string.Empty;
    public long Bytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, string> Transformations { get; set; } = new();
}