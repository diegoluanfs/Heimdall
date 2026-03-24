using Heimdall.Domain.Entities;

namespace Heimdall.Domain.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetActiveByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default);
    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
