using Nemui.Shared.DTOs.Common;
using Microsoft.AspNetCore.Http;

namespace Nemui.Application.Services;

public interface IImageService
{
    Task<ImageResponse> UploadImageAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);
    Task<ImageResponse> UploadUserAvatarAsync(IFormFile file, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteImageAsync(string publicId, CancellationToken cancellationToken = default);
    Task<string> GetImageUrlWithTransformationAsync(string publicId, string transformation, CancellationToken cancellationToken = default);
    Task<bool> ValidateImageFileAsync(IFormFile file);
}