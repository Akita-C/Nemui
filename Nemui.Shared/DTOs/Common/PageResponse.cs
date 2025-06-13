namespace Nemui.Shared.DTOs.Common;

public class PagedResponse<T>
{
    public List<T> Data { get; set; } = [];
    public string? NextCursor { get; set; }
    public bool HasNextPage { get; set; }
    public int Count => Data.Count;
}