using Heimdall.Application.DTOs;

namespace Heimdall.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, string userAgent, string ip, CancellationToken ct = default);
    Task<LoginResponse?> RefreshAsync(RefreshRequest request, string userAgent, string ip, CancellationToken ct = default);
    Task<bool> RevokeAsync(RevokeRequest request, CancellationToken ct = default);
}
