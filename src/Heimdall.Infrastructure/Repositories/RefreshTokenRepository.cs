using Heimdall.Domain.Entities;
using Heimdall.Domain.Interfaces;
using Heimdall.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Heimdall.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly HeimdallDbContext _context;

    public RefreshTokenRepository(HeimdallDbContext context)
    {
        _context = context;
    }

    public async Task<RefreshToken?> GetActiveByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.RefreshTokenHash == tokenHash && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow, cancellationToken);

    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default)
        => await _context.RefreshTokens.AddAsync(token, cancellationToken);

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
            token.RevokedAt = DateTime.UtcNow;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
