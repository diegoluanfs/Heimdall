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
            var response = await _httpClient.PostAsJsonAsync("/api/login", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<LoginResponse>();
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }
}
