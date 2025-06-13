using Microsoft.EntityFrameworkCore;
using Nemui.Application.Common.Interfaces;
using Nemui.Infrastructure.Data.Context;
using Nemui.Shared.Entities;

namespace Nemui.Infrastructure.Data.Repositories.Implementations;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetWithRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    public async Task UpdateLastLoginAsync(Guid userId, DateTime loginTime, CancellationToken cancellationToken = default)
    {
        await _dbSet
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.LastLoginAt, loginTime), cancellationToken);
    }

    public async Task IncrementFailedLoginAttemptsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _dbSet
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.FailedLoginAttempts, u => u.FailedLoginAttempts + 1), cancellationToken);
    }

    public async Task ResetFailedLoginAttemptsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _dbSet
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.FailedLoginAttempts, 0)
                .SetProperty(u => u.LockoutEnd, (DateTime?)null), cancellationToken);
    }

    public async Task SetLockoutAsync(Guid userId, DateTime? lockoutEnd, CancellationToken cancellationToken = default)
    {
        await _dbSet
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.LockoutEnd, lockoutEnd), cancellationToken);
    }
}