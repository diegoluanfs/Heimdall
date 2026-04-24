using Heimdall.Application.DTOs;
using Heimdall.Application.Interfaces;
using Heimdall.Domain.Entities;
using Heimdall.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Heimdall.Application.Services;

public class UserService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository users, IPasswordHasher passwordHasher, ILogger<UserService> logger)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<Guid?> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Attempting to create user with email: {Email}", request.Email);

        var existing = await _users.GetByEmailAsync(request.Email, ct);
        if (existing is not null)
        {
            _logger.LogWarning("User creation failed - Email already exists: {Email}", request.Email);
            return null;
        }

        var user = new User
        {
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = _passwordHasher.Hash(request.Password)
        };

        await _users.AddAsync(user, ct);
        await _users.SaveChangesAsync(ct);

        _logger.LogInformation("User created successfully: {UserId}, email: {Email}", user.Id, user.Email);
        return user.Id;
    }
}
