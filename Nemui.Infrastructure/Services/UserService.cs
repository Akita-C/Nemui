using System.ComponentModel.DataAnnotations;
using Nemui.Application.Common.Interfaces;
using Nemui.Application.Services;
using Nemui.Shared.DTOs.Auth;

namespace Nemui.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly IUserCacheService _userCacheService;
    
    public UserService(
        IUnitOfWork unitOfWork,
        IPasswordService passwordService, 
        IUserCacheService userCacheService)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _userCacheService = userCacheService;
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
            LastLoginAt = user.LastLoginAt
        };
        
        await _userCacheService.SetUserProfileAsync(userId, profile, cancellationToken);
        
        return profile;
    }

    public async Task<UserProfileDto?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken);
        if (user == null) return null;

        return new UserProfileDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            IsEmailVerified = user.IsEmailVerified,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
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
}