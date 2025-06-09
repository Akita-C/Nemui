namespace Nemui.Application.Services;

public interface IJwtBlacklistService
{
    Task BlacklistTokenAsync(string token, CancellationToken cancellationToken);
    Task BlacklistTokenAsync(string token, TimeSpan expiry, CancellationToken cancellationToken = default);
    Task<bool> IsTokenBlacklistedAsync(string token, CancellationToken cancellationToken = default);
    Task BlacklistAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default);
    Task CleanupExpiredTokensAsync(CancellationToken cancellationToken = default);
}