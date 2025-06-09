using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nemui.Application.Services;
using Nemui.Infrastructure.Configurations;
using Nemui.Shared.DTOs.Common;

namespace Nemui.Infrastructure.Services;

public class CloudinaryImageService : IImageService
{
    private readonly Cloudinary _cloudinary;
    private readonly CloudinarySettings _settings;
    private readonly ILogger<CloudinaryImageService> _logger;
    
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private const int MaxFileSizeBytes = 10 * 1024 * 1024; // 10Mb

    public CloudinaryImageService(IOptions<CloudinarySettings> settings, ILogger<CloudinaryImageService> logger)
    {
        _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger;
        var account = new Account(_settings.CloudName, _settings.ApiKey, _settings.ApiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<ImageResponse> UploadImageAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
    {
        if (!await ValidateImageFileAsync(file)) throw new ArgumentException("Invalid image file");

        try
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, file.OpenReadStream()),
                Folder = folder,
                UseFilename = false,
                UniqueFilename = true,
                Overwrite = false,
                PublicId = $"{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(file.FileName)}",
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                _logger.LogError("Cloudinary upload error: {Error}", uploadResult.Error.Message);
                throw new InvalidOperationException($"Image upload failed: {uploadResult.Error.Message}");
            }
            
            _logger.LogInformation("Image uploaded successfully: {PublicId}", uploadResult.PublicId);

            return new ImageResponse
            {
                PublicId = uploadResult.PublicId,
                Url = uploadResult.Url.ToString(),
                SecureUrl = uploadResult.SecureUrl.ToString(),
                Width = uploadResult.Width,
                Height = uploadResult.Height,
                Format = uploadResult.Format,
                Bytes = uploadResult.Bytes,
                CreatedAt = uploadResult.CreatedAt,
                Transformations = GenerateTransformUrls(uploadResult.PublicId)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image to Cloudinary");
            throw;
        }
    }

    public async Task<ImageResponse> UploadUserAvatarAsync(IFormFile file, Guid userId, CancellationToken cancellationToken = default)
    {
        var folder = _settings.Folders.UserAvatars;
        
        // Tạo publicId có format user-specific để dễ quản lý
        var customPublicId = $"user_{userId}_avatar_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        
        if (!await ValidateImageFileAsync(file))
        {
            throw new ArgumentException("Invalid image file");
        }

        try
        {
            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(file.FileName, file.OpenReadStream()),
                Folder = folder,
                PublicId = customPublicId,
                Overwrite = false,
                UseFilename = false,
                UniqueFilename = false,
                Transformation = new Transformation()
                    .Width(500).Height(500)
                    .Crop("fill")
                    .Gravity("face")
                    .Quality("auto")
                    .FetchFormat("auto")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                _logger.LogError("Avatar upload error for user {UserId}: {Error}", userId, uploadResult.Error.Message);
                throw new InvalidOperationException($"Avatar upload failed: {uploadResult.Error.Message}");
            }

            _logger.LogInformation("Avatar uploaded successfully for user {UserId}: {PublicId}", userId, uploadResult.PublicId);

            return new ImageResponse
            {
                PublicId = uploadResult.PublicId,
                Url = uploadResult.Url.ToString(),
                SecureUrl = uploadResult.SecureUrl.ToString(),
                Width = uploadResult.Width,
                Height = uploadResult.Height,
                Format = uploadResult.Format,
                Bytes = uploadResult.Bytes,
                CreatedAt = uploadResult.CreatedAt,
                Transformations = GenerateAvatarTransformationUrls(uploadResult.PublicId)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading avatar for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> DeleteImageAsync(string publicId, CancellationToken cancellationToken = default)
    {
        try
        {
            var deleteParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deleteParams);
            
            var success = result.Result == "ok";
            
            if (success)
            {
                _logger.LogInformation("Image deleted successfully: {PublicId}", publicId);
            }
            else
            {
                _logger.LogWarning("Failed to delete image: {PublicId}, Result: {Result}", publicId, result.Result);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image: {PublicId}", publicId);
            return false;
        }
    }

    public Task<string> GetImageUrlWithTransformationAsync(string publicId, string transformation,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = _cloudinary.Api.UrlImgUp.Transform(new Transformation().RawTransformation(transformation))
                .BuildUrl(publicId);
            return Task.FromResult(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating transformed URL for {PublicId}", publicId);
            throw;
        }
    }

    public Task<bool> ValidateImageFileAsync(IFormFile file)
    {
        if (file.Length == 0)
        {
            return Task.FromResult(false);
        }

        // Check file size
        if (file.Length > MaxFileSizeBytes)
        {
            return Task.FromResult(false);
        }

        // Check file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
        {
            return Task.FromResult(false);
        }

        // Check MIME type
        var allowedMimeTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
        if (!allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    private Dictionary<string, string> GenerateTransformUrls(string publicId)
    {
        var transformations = new Dictionary<string, string>();

        try
        {
            transformations.Add("small", _cloudinary.Api.UrlImgUp.Transform(new Transformation().RawTransformation(_settings.Transformations.DocumentThumbnail)).BuildUrl(publicId));
            transformations.Add("original", _cloudinary.Api.UrlImgUp.BuildUrl(publicId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating transformation URLs for {PublicId}", publicId);
        }
        
        return transformations;
    }
    
    private Dictionary<string, string> GenerateAvatarTransformationUrls(string publicId)
    {
        var transformations = new Dictionary<string, string>();
        
        try
        {
            transformations.Add("small", _cloudinary.Api.UrlImgUp.Transform(new Transformation().RawTransformation(_settings.Transformations.AvatarSmall)).BuildUrl(publicId));
            transformations.Add("medium", _cloudinary.Api.UrlImgUp.Transform(new Transformation().RawTransformation(_settings.Transformations.AvatarMedium)).BuildUrl(publicId));
            transformations.Add("large", _cloudinary.Api.UrlImgUp.Transform(new Transformation().RawTransformation(_settings.Transformations.AvatarLarge)).BuildUrl(publicId));
            transformations.Add("original", _cloudinary.Api.UrlImgUp.BuildUrl(publicId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating avatar transformation URLs for {PublicId}", publicId);
        }
        
        return transformations;
    }
}