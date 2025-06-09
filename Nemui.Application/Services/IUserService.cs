using Nemui.Shared.DTOs.Auth;
using Nemui.Shared.DTOs.Common;

namespace Nemui.Application.Services;

public interface IUserService
{
    Task<UserProfileDto?> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserProfileDto?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> UpdateUserProfileAsync(Guid userId, UpdateUserProfileRequest request, CancellationToken cancellationToken = default);
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ActivateUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<ImageResponse> UpdateUserAvatarAsync(Guid userId, UpdateAvatarRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserAvatarAsync(Guid userId, CancellationToken cancellationToken = default);
}