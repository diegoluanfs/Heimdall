using Heimdall.Domain.Entities;
using Heimdall.Domain.Interfaces;
using Heimdall.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Heimdall.Infrastructure.Repositories;

public class UserProjectRepository : IUserProjectRepository
{
    private readonly HeimdallDbContext _context;

    public UserProjectRepository(HeimdallDbContext context)
    {
        _context = context;
    }

    public async Task<UserProject?> GetAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default)
        => await _context.UserProjects
            .Include(up => up.Project)
            .FirstOrDefaultAsync(up => up.UserId == userId && up.ProjectId == projectId, cancellationToken);

    public async Task AddAsync(UserProject userProject, CancellationToken cancellationToken = default)
        => await _context.UserProjects.AddAsync(userProject, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
