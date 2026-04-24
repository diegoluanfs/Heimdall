using Heimdall.Application.DTOs;
using Heimdall.Application.Interfaces;
using Heimdall.Domain.Entities;
using Heimdall.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Heimdall.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IProjectRepository _projects;
    private readonly IUserProjectRepository _userProjects;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository users,
        IProjectRepository projects,
        IUserProjectRepository userProjects,
        IRefreshTokenRepository refreshTokens,
        ITokenService tokenService,
        IPasswordHasher passwordHasher,
        ILogger<AuthService> logger)
    {
        _users = users;
        _projects = projects;
        _userProjects = userProjects;
        _refreshTokens = refreshTokens;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, string userAgent, string ip, CancellationToken ct = default)
    {
        _logger.LogInformation("Login attempt for email: {Email}, audience: {Audience}, IP: {IP}", 
            request.Email, request.Audience, ip);

        var user = await _users.GetByEmailAsync(request.Email, ct);
        if (user is null || !user.IsActive)
        {
            _logger.LogWarning("Login failed - User not found or inactive: {Email}, IP: {IP}", 
                request.Email, ip);
            return null;
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed - Invalid password for user: {UserId}, email: {Email}, IP: {IP}", 
                user.Id, request.Email, ip);
            return null;
        }

        var project = await _projects.GetByAudienceAsync(request.Audience, ct);
        if (project is null || !project.IsActive)
        {
            _logger.LogWarning("Login failed - Project not found or inactive: {Audience}, user: {UserId}, IP: {IP}", 
                request.Audience, user.Id, ip);
            return null;
        }

        var userProject = await _userProjects.GetAsync(user.Id, project.Id, ct);
        if (userProject is null)
        {
            _logger.LogWarning("Login failed - User not associated with project: {UserId}, project: {ProjectId}, IP: {IP}", 
                user.Id, project.Id, ip);
            return null;
        }

        var accessToken = _tokenService.GenerateAccessToken(user, project, userProject.Role);
        var (rawToken, tokenHash) = _tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            ProjectId = project.Id,
            RefreshTokenHash = tokenHash,
            UserAgent = userAgent,
            Ip = ip,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await _refreshTokens.AddAsync(refreshToken, ct);
        await _refreshTokens.SaveChangesAsync(ct);

        _logger.LogInformation("Login successful for user: {UserId}, email: {Email}, project: {ProjectId}, role: {Role}, IP: {IP}", 
            user.Id, request.Email, project.Id, userProject.Role, ip);

        return new LoginResponse(accessToken, rawToken, 300);
    }

    public async Task<LoginResponse?> RefreshAsync(RefreshRequest request, string userAgent, string ip, CancellationToken ct = default)
    {
        _logger.LogInformation("Token refresh attempt from IP: {IP}", ip);

        var tokenHash = _tokenService.HashToken(request.RefreshToken);
        var storedToken = await _refreshTokens.GetActiveByHashAsync(tokenHash, ct);

        if (storedToken is null || !storedToken.IsActive)
        {
            _logger.LogWarning("Token refresh failed - Invalid or inactive refresh token from IP: {IP}", ip);
            return null;
        }

        storedToken.RevokedAt = DateTime.UtcNow;
        await _refreshTokens.SaveChangesAsync(ct);

        var user = await _users.GetByIdAsync(storedToken.UserId, ct);
        if (user is null || !user.IsActive)
        {
            _logger.LogWarning("Token refresh failed - User not found or inactive: {UserId}, IP: {IP}", 
                storedToken.UserId, ip);
            return null;
        }

        var userProject = await _userProjects.GetAsync(user.Id, storedToken.ProjectId, ct);
        if (userProject is null)
        {
            _logger.LogWarning("Token refresh failed - User not associated with project: {UserId}, project: {ProjectId}, IP: {IP}", 
                user.Id, storedToken.ProjectId, ip);
            return null;
        }

        var project = userProject.Project;
        var accessToken = _tokenService.GenerateAccessToken(user, project, userProject.Role);
        var (rawToken, newHash) = _tokenService.GenerateRefreshToken();

        var newRefreshToken = new RefreshToken
        {
            UserId = user.Id,
            ProjectId = storedToken.ProjectId,
            RefreshTokenHash = newHash,
            UserAgent = userAgent,
            Ip = ip,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await _refreshTokens.AddAsync(newRefreshToken, ct);
        await _refreshTokens.SaveChangesAsync(ct);

        _logger.LogInformation("Token refresh successful for user: {UserId}, project: {ProjectId}, IP: {IP}", 
            user.Id, project.Id, ip);

        return new LoginResponse(accessToken, rawToken, 300);
    }

    public async Task<bool> RevokeAsync(RevokeRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Token revocation attempt");

        var tokenHash = _tokenService.HashToken(request.RefreshToken);
        var storedToken = await _refreshTokens.GetActiveByHashAsync(tokenHash, ct);

        if (storedToken is null)
        {
            _logger.LogWarning("Token revocation failed - Token not found");
            return false;
        }

        storedToken.RevokedAt = DateTime.UtcNow;
        await _refreshTokens.SaveChangesAsync(ct);

        _logger.LogInformation("Token revoked successfully for user: {UserId}, project: {ProjectId}", 
            storedToken.UserId, storedToken.ProjectId);

        return true;
    }
}
