using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Nemui.Shared.DTOs.Common;

public class ImageUploadRequest
{
    [Required]
    public IFormFile File { get; set; } = null!;
    
    public string? Folder { get; set; }
    
    public bool GenerateThumbnails { get; set; } = true;
}