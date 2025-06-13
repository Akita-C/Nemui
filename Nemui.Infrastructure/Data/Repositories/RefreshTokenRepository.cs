using Microsoft.EntityFrameworkCore;
using Nemui.Application.Common.Interfaces;
using Nemui.Infrastructure.Data.Context;
using Nemui.Shared.Entities;

namespace Nemui.Infrastructure.Data.Repositories.Implementations;

public class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);
    }

    public async Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var currentTime = DateTime.UtcNow;

        return await _dbSet
            .Where(rt => rt.UserId == userId &&
                         rt.RevokedAt == null &&
                         rt.ExpiresAt > currentTime)
            .ToListAsync(cancellationToken);
    }

    public async Task RevokeTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        await _dbSet
            .Where(rt => rt.Token == token)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(rt => rt.RevokedAt, DateTime.UtcNow), cancellationToken);
    }

    public async Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _dbSet
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(rt => rt.RevokedAt, DateTime.UtcNow), cancellationToken);
    }

    public async Task CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        var expiredDate = DateTime.UtcNow.AddDays(-30);

        await _dbSet
            .Where(rt => rt.ExpiresAt < expiredDate)
            .ExecuteDeleteAsync(cancellationToken);
    }
}