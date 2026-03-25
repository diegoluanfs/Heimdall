using System.Threading;
using System.Threading.Tasks;
using Heimdall.Application.DTOs;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, string userAgent, string ip, CancellationToken ct);
}

public class MockAuthService : IAuthService
{
    // Usuário e senha fixos para teste
    private const string TestEmail = "admin@heimdall.com";
    private const string TestPassword = "123456";

    public Task<LoginResponse?> LoginAsync(LoginRequest request, string userAgent, string ip, CancellationToken ct)
    {
        if (request.Email == TestEmail && request.Password == TestPassword)
        {
            return Task.FromResult<LoginResponse?>(
                new LoginResponse("mocked-token", "mocked-refresh-token", 3600)
            );
        }
        return Task.FromResult<LoginResponse?>(null);
    }
}