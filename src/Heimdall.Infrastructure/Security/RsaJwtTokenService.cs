using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Heimdall.Application.Interfaces;
using Heimdall.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Heimdall.Infrastructure.Security;

/// <summary>
/// JWT RS256 token service. Generates access tokens (5 min) and refresh tokens (7 days).
/// </summary>
public class RsaJwtTokenService : ITokenService, IDisposable
{
    private readonly RSA _privateKey;
    private readonly string _issuer;

    public RsaJwtTokenService(IConfiguration configuration)
    {
        _issuer = configuration["Jwt:Issuer"] ?? "heimdall";
        var privateKeyPem = configuration["Jwt:PrivateKeyPem"]
            ?? throw new InvalidOperationException("Jwt:PrivateKeyPem configuration is required.");

        _privateKey = RSA.Create();
        _privateKey.ImportFromPem(privateKeyPem);
    }

    public string GenerateAccessToken(User user, Project project, string role)
    {
        var credentials = new SigningCredentials(
            new RsaSecurityKey(_privateKey),
            SecurityAlgorithms.RsaSha256);

        var now = DateTime.UtcNow;
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("project", project.Audience),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: project.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(5),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (string rawToken, string tokenHash) GenerateRefreshToken()
    {
        var rawBytes = RandomNumberGenerator.GetBytes(64);
        var rawToken = Convert.ToBase64String(rawBytes);
        var tokenHash = HashToken(rawToken);
        return (rawToken, tokenHash);
    }

    public string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    public void Dispose() => _privateKey.Dispose();
}
