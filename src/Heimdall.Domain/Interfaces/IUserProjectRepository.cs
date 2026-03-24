using Heimdall.Domain.Entities;

namespace Heimdall.Domain.Interfaces;

public interface IUserProjectRepository
{
    Task<UserProject?> GetAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default);
    Task AddAsync(UserProject userProject, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
