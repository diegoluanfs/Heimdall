using Heimdall.Application.DTOs;
using Heimdall.Domain.Entities;
using Heimdall.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Heimdall.Application.Services;

public class ProjectService
{
    private readonly IProjectRepository _projects;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(IProjectRepository projects, ILogger<ProjectService> logger)
    {
        _projects = projects;
        _logger = logger;
    }

    public async Task<Guid?> CreateProjectAsync(CreateProjectRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Attempting to create project: {Name} with audience: {Audience}", 
            request.Name, request.Audience);

        var existing = await _projects.GetByAudienceAsync(request.Audience, ct);
        if (existing is not null)
        {
            _logger.LogWarning("Project creation failed - Audience already exists: {Audience}", request.Audience);
            return null;
        }

        var project = new Project
        {
            Name = request.Name,
            Audience = request.Audience
        };

        await _projects.AddAsync(project, ct);
        await _projects.SaveChangesAsync(ct);

        _logger.LogInformation("Project created successfully: {ProjectId}, name: {Name}, audience: {Audience}", 
            project.Id, project.Name, project.Audience);
        return project.Id;
    }
}
