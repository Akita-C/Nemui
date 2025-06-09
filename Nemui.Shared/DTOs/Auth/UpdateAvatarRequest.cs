using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Nemui.Shared.DTOs.Auth;

public class UpdateAvatarRequest
{
    [Required]
    public IFormFile Avatar { get; set; } = null!;
}