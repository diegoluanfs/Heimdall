using Heimdall.Domain.Entities;

namespace Heimdall.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user, Project project, string role);
    (string rawToken, string tokenHash) GenerateRefreshToken();
    string HashToken(string token);
}
