using Heimdall.Application.DTOs;
using Heimdall.Application.Interfaces;
using Heimdall.Domain.Entities;
using Heimdall.Domain.Interfaces;

namespace Heimdall.Application.Services;

public class UserService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IUserRepository users, IPasswordHasher passwordHasher)
    {
        _users = users;
        _passwordHasher = passwordHasher;
    }

    public async Task<Guid?> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var existing = await _users.GetByEmailAsync(request.Email, ct);
        if (existing is not null)
            return null;

        var user = new User
        {
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = _passwordHasher.Hash(request.Password)
        };

        await _users.AddAsync(user, ct);
        await _users.SaveChangesAsync(ct);
        return user.Id;
    }
}
