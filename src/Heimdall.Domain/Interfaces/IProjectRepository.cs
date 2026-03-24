using Heimdall.Domain.Entities;

namespace Heimdall.Domain.Interfaces;

public interface IProjectRepository
{
    Task<Project?> GetByAudienceAsync(string audience, CancellationToken cancellationToken = default);
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Project project, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
