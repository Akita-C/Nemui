namespace Nemui.Shared.DTOs.Common;

public class BaseApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = [];
}