using System.Net.Http.Json;
using System.Text.Json;

namespace Heimdall.Web.Services;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
}

public class AuthService
{
    private readonly HttpClient _httpClient;
    
    public AuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<LoginResponse?> LoginAsync(string email, string password, string audience)
    {
        var request = new LoginRequest
        {
            Email = email,
            Password = password,
            Audience = audience
        };

        try
        {
            Console.WriteLine($"[AuthService] Sending login request to: {_httpClient.BaseAddress}api/login");
            Console.WriteLine($"[AuthService] Email: {email}, Audience: {audience}");

            var response = await _httpClient.PostAsJsonAsync("/api/login", request);

            Console.WriteLine($"[AuthService] Response status: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                Console.WriteLine("[AuthService] Login successful!");
                return result;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[AuthService] Login failed: {errorContent}");

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthService] Exception during login: {ex.GetType().Name}");
            Console.WriteLine($"[AuthService] Exception message: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"[AuthService] Inner exception: {ex.InnerException.Message}");
            }
            return null;
        }
    }
}
