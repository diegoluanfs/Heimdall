using Heimdall.Application.DTOs;
using Heimdall.Domain.Entities;
using Heimdall.Domain.Interfaces;

namespace Heimdall.Application.Services;

public class ProjectService
{
    private readonly IProjectRepository _projects;

    public ProjectService(IProjectRepository projects)
    {
        _projects = projects;
    }

    public async Task<Guid?> CreateProjectAsync(CreateProjectRequest request, CancellationToken ct = default)
    {
        var existing = await _projects.GetByAudienceAsync(request.Audience, ct);
        if (existing is not null)
            return null;

        var project = new Project
        {
            Name = request.Name,
            Audience = request.Audience
        };

        await _projects.AddAsync(project, ct);
        await _projects.SaveChangesAsync(ct);
        return project.Id;
    }
}
