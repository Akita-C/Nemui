using System.ComponentModel.DataAnnotations;
using Nemui.Application.Common.Interfaces;
using Nemui.Application.Repositories;
using Nemui.Application.Services;
using Nemui.Shared.DTOs.Auth;
using Nemui.Shared.DTOs.Common;

namespace Nemui.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly IUserCacheService _userCacheService;
    private readonly IImageService _imageService;
    
    public UserService(
        IUnitOfWork unitOfWork,
        IPasswordService passwordService, 
        IUserCacheService userCacheService, IImageService imageService)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _userCacheService = userCacheService;
        _imageService = imageService;
    }
    
    public async Task<UserProfileDto?> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cachedProfile = await _userCacheService.GetUserProfileAsync(userId, cancellationToken);
        if (cachedProfile != null) return cachedProfile;
        
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null) return null;
        var profile = new UserProfileDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            IsEmailVerified = user.IsEmailVerified,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            AvatarUrl = user.AvatarUrl,
        };

        if (!string.IsNullOrEmpty(user.AvatarPublicId))
        {
            profile.AvatarTransformations = new Dictionary<string, string>
            {
                ["small"] = await _imageService.GetImageUrlWithTransformationAsync(user.AvatarPublicId,
                    "c_fill,w_150,h_150,q_auto,f_auto", cancellationToken),
                ["medium"] = await _imageService.GetImageUrlWithTransformationAsync(user.AvatarPublicId,
                    "c_fill,w_300,h_300,q_auto,f_auto", cancellationToken),
                ["large"] = await _imageService.GetImageUrlWithTransformationAsync(user.AvatarPublicId,
                    "c_fill,w_500,h_500,q_auto,f_auto", cancellationToken)
            };
        }
        
        await _userCacheService.SetUserProfileAsync(userId, profile, cancellationToken);
        
        return profile;
    }

    public async Task<UserProfileDto?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken);
        if (user == null) return null;

        var profile = new UserProfileDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            IsEmailVerified = user.IsEmailVerified,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
        
        if (!string.IsNullOrEmpty(user.AvatarPublicId))
        {
            profile.AvatarTransformations = new Dictionary<string, string>
            {
                ["small"] = await _imageService.GetImageUrlWithTransformationAsync(user.AvatarPublicId, "c_fill,w_150,h_150,q_auto,f_auto", cancellationToken),
                ["medium"] = await _imageService.GetImageUrlWithTransformationAsync(user.AvatarPublicId, "c_fill,w_300,h_300,q_auto,f_auto", cancellationToken),
                ["large"] = await _imageService.GetImageUrlWithTransformationAsync(user.AvatarPublicId, "c_fill,w_500,h_500,q_auto,f_auto", cancellationToken)
            };
        }
        
        return profile;
    }

    public async Task<bool> UpdateUserProfileAsync(Guid userId, UpdateUserProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null) return false;

        user.Name = request.Name.Trim();
        
        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _userCacheService.InvalidateUserProfileAsync(userId, cancellationToken);
        
        return true;
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null) return false;

        // Verify current password
        if (!_passwordService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect");

        // Validate new password
        if (request.NewPassword != request.ConfirmNewPassword)
            throw new ValidationException("New passwords do not match");

        if (!_passwordService.IsPasswordStrong(request.NewPassword))
            throw new ValidationException("New password does not meet security requirements");

        // Update password
        user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
        
        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
        
        // Revoke all refresh tokens for security
        await _unitOfWork.RefreshTokens.RevokeAllUserTokensAsync(userId, cancellationToken);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        // Invalidate all user cache data for security
        await _userCacheService.InvalidateAllUserDataAsync(userId, cancellationToken);
        
        return true;
    }

    public async Task<bool> DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null) return false;

        user.IsActive = false;
        
        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
        
        // Revoke all refresh tokens
        await _unitOfWork.RefreshTokens.RevokeAllUserTokensAsync(userId, cancellationToken);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        // Invalidate all user cache data
        await _userCacheService.InvalidateAllUserDataAsync(userId, cancellationToken);
        
        return true;
    }

    public async Task<bool> ActivateUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null) return false;

        user.IsActive = true;
        
        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        // Invalidate cache to refresh data
        await _userCacheService.InvalidateUserProfileAsync(userId, cancellationToken);
        
        return true;
    }

    public async Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Users.ExistsAsync(u => u.Id == userId, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Users.ExistsByEmailAsync(email, cancellationToken);
    }

    public async Task<ImageResponse> UpdateUserAvatarAsync(Guid userId, UpdateAvatarRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null) throw new InvalidOperationException("User not found");

        // Delete old avatar if exists
        if (!string.IsNullOrEmpty(user.AvatarPublicId))
        {
            await _imageService.DeleteImageAsync(user.AvatarPublicId, cancellationToken);
        }

        // Upload new avatar
        var imageResponse = await _imageService.UploadUserAvatarAsync(request.Avatar, userId, cancellationToken);

        // Update user avatar information
        user.AvatarPublicId = imageResponse.PublicId;
        user.AvatarUrl = imageResponse.SecureUrl;

        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        await _userCacheService.InvalidateUserProfileAsync(userId, cancellationToken);

        return imageResponse;
    }

    public async Task<bool> DeleteUserAvatarAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null || string.IsNullOrEmpty(user.AvatarPublicId)) return false;

        // Delete from Cloudinary
        var deleted = await _imageService.DeleteImageAsync(user.AvatarPublicId, cancellationToken);
        
        if (deleted)
        {
            // Clear avatar info from database
            user.AvatarPublicId = null;
            user.AvatarUrl = null;

            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Invalidate cache
            await _userCacheService.InvalidateUserProfileAsync(userId, cancellationToken);
        }

        return deleted;
    }
}