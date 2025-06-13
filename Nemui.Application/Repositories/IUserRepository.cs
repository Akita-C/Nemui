using Nemui.Application.Repositories;
using Nemui.Shared.Entities;

namespace Nemui.Application.Common.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetWithRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default);
    Task UpdateLastLoginAsync(Guid userId, DateTime loginTime, CancellationToken cancellationToken = default);
    Task IncrementFailedLoginAttemptsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task ResetFailedLoginAttemptsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SetLockoutAsync(Guid userId, DateTime? lockoutEnd, CancellationToken cancellationToken = default);
}