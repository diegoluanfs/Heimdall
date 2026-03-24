using Heimdall.Application.DTOs;
using Heimdall.Application.Interfaces;
using Heimdall.Domain.Entities;
using Heimdall.Domain.Interfaces;

namespace Heimdall.Application.Services;

public class AuthService
{
    private readonly IUserRepository _users;
    private readonly IProjectRepository _projects;
    private readonly IUserProjectRepository _userProjects;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(
        IUserRepository users,
        IProjectRepository projects,
        IUserProjectRepository userProjects,
        IRefreshTokenRepository refreshTokens,
        ITokenService tokenService,
        IPasswordHasher passwordHasher)
    {
        _users = users;
        _projects = projects;
        _userProjects = userProjects;
        _refreshTokens = refreshTokens;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, string userAgent, string ip, CancellationToken ct = default)
    {
        var user = await _users.GetByEmailAsync(request.Email, ct);
        if (user is null || !user.IsActive)
            return null;

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            return null;

        var project = await _projects.GetByAudienceAsync(request.Audience, ct);
        if (project is null || !project.IsActive)
            return null;

        var userProject = await _userProjects.GetAsync(user.Id, project.Id, ct);
        if (userProject is null)
            return null;

        var accessToken = _tokenService.GenerateAccessToken(user, project, userProject.Role);
        var (rawToken, tokenHash) = _tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            RefreshTokenHash = tokenHash,
            UserAgent = userAgent,
            Ip = ip,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await _refreshTokens.AddAsync(refreshToken, ct);
        await _refreshTokens.SaveChangesAsync(ct);

        return new LoginResponse(accessToken, rawToken, 300);
    }

    public async Task<LoginResponse?> RefreshAsync(RefreshRequest request, string userAgent, string ip, CancellationToken ct = default)
    {
        var tokenHash = _tokenService.HashToken(request.RefreshToken);
        var storedToken = await _refreshTokens.GetActiveByHashAsync(tokenHash, ct);

        if (storedToken is null || !storedToken.IsActive)
            return null;

        storedToken.RevokedAt = DateTime.UtcNow;
        await _refreshTokens.SaveChangesAsync(ct);

        var user = await _users.GetByIdAsync(storedToken.UserId, ct);
        if (user is null || !user.IsActive)
            return null;

        var userProject = user.UserProjects.FirstOrDefault();
        if (userProject is null)
            return null;

        var project = userProject.Project;
        var accessToken = _tokenService.GenerateAccessToken(user, project, userProject.Role);
        var (rawToken, newHash) = _tokenService.GenerateRefreshToken();

        var newRefreshToken = new RefreshToken
        {
            UserId = user.Id,
            RefreshTokenHash = newHash,
            UserAgent = userAgent,
            Ip = ip,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await _refreshTokens.AddAsync(newRefreshToken, ct);
        await _refreshTokens.SaveChangesAsync(ct);

        return new LoginResponse(accessToken, rawToken, 300);
    }

    public async Task<bool> RevokeAsync(RevokeRequest request, CancellationToken ct = default)
    {
        var tokenHash = _tokenService.HashToken(request.RefreshToken);
        var storedToken = await _refreshTokens.GetActiveByHashAsync(tokenHash, ct);

        if (storedToken is null)
            return false;

        storedToken.RevokedAt = DateTime.UtcNow;
        await _refreshTokens.SaveChangesAsync(ct);
        return true;
    }
}
