using Heimdall.Domain.Entities;
using Heimdall.Domain.Interfaces;
using Heimdall.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Heimdall.Infrastructure.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly HeimdallDbContext _context;

    public ProjectRepository(HeimdallDbContext context)
    {
        _context = context;
    }

    public async Task<Project?> GetByAudienceAsync(string audience, CancellationToken cancellationToken = default)
        => await _context.Projects.FirstOrDefaultAsync(p => p.Audience == audience, cancellationToken);

    public async Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task AddAsync(Project project, CancellationToken cancellationToken = default)
        => await _context.Projects.AddAsync(project, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
