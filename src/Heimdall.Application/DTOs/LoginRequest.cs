namespace Heimdall.Application.DTOs;

public record LoginRequest(string Email, string Password, string Audience);
