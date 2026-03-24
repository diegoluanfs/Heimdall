using Heimdall.Domain.Entities;
using Heimdall.Domain.Interfaces;
using Heimdall.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Heimdall.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly HeimdallDbContext _context;

    public UserRepository(HeimdallDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await _context.Users
            .Include(u => u.UserProjects).ThenInclude(up => up.Project)
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Users
            .Include(u => u.UserProjects).ThenInclude(up => up.Project)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        => await _context.Users.AddAsync(user, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
