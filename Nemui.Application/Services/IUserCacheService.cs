using Nemui.Shared.DTOs.Auth;

namespace Nemui.Application.Services;

public interface IUserCacheService
{
    Task<UserProfileDto?> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SetUserProfileAsync(Guid userId, UserProfileDto userProfile, CancellationToken cancellationToken = default);
    Task InvalidateUserProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task InvalidateAllUserDataAsync(Guid userId, CancellationToken cancellationToken = default);
}