using System.Security.Cryptography;
using System.Threading.RateLimiting;
using Heimdall.Application.DTOs;
using Heimdall.Application.Interfaces;
using Heimdall.Application.Services;
using Heimdall.Infrastructure.Data;
using Heimdall.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ──────────────────────────────── Security headers / HSTS / CORS ────────────────────────────────
builder.Services.AddHsts(o =>
{
    o.Preload = true;
    o.IncludeSubDomains = true;
    o.MaxAge = TimeSpan.FromDays(365);
});

//Temos que ajustar em produção para permitir apenas os domínios autorizados, mas para desenvolvimento local é mais fácil permitir tudo
builder.Services.AddCors(o =>
{
    o.AddDefaultPolicy(p =>
    {
        p.AllowAnyOrigin()
         .AllowAnyMethod()
         .AllowAnyHeader();
    });
});

// ──────────────────────────────── Rate limiting ────────────────────────────────
builder.Services.AddRateLimiter(o =>
{
    o.AddFixedWindowLimiter("login", opts =>
    {
        opts.PermitLimit = 10;
        opts.Window = TimeSpan.FromMinutes(1);
        opts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opts.QueueLimit = 0;
    });
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// ──────────────────────────────── JWT RS256 Authentication ────────────────────────────────
var publicKeyPem = builder.Configuration["Jwt:PublicKeyPem"]
    ?? throw new InvalidOperationException("Jwt:PublicKeyPem configuration is required.");

var rsa = RSA.Create();
rsa.ImportFromPem(publicKeyPem);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudiences = builder.Configuration.GetSection("Jwt:ValidAudiences").Get<string[]>(),
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(rsa),
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();

// ──────────────────────────────── Infrastructure ────────────────────────────────
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// ──────────────────────────────── Middleware pipeline ────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// Servir arquivos estáticos do Blazor WebAssembly
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// Security headers
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    ctx.Response.Headers.Append("X-Frame-Options", "DENY");
    ctx.Response.Headers.Append("X-XSS-Protection", "0");
    ctx.Response.Headers.Append("Referrer-Policy", "no-referrer");

    // CSP ajustado para permitir Blazor WebAssembly
    if (!ctx.Request.Path.StartsWithSegments("/api"))
    {
        ctx.Response.Headers.Append("Content-Security-Policy", 
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-eval' 'wasm-unsafe-eval'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data:; " +
            "connect-src 'self';");
    }
    else
    {
        ctx.Response.Headers.Append("Content-Security-Policy", "default-src 'none'");
    }

    ctx.Response.Headers.Append("Permissions-Policy", "geolocation=(), camera=(), microphone=()");
    await next();
});

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// ──────────────────────────────── Auto-migrate on startup ────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HeimdallDbContext>();
    db.Database.Migrate();

    // Criar usuário admin padrão se não existir
    var userService = scope.ServiceProvider.GetRequiredService<UserService>();
    var projectService = scope.ServiceProvider.GetRequiredService<ProjectService>();

    var adminEmail = "admin@heimdall.com";
    var adminPassword = "Admin@123"; // Trocar em produção via variável de ambiente

    // Criar usuário admin
    var adminUserId = await userService.CreateUserAsync(
        new CreateUserRequest(adminEmail, adminPassword), 
        CancellationToken.None);

    if (adminUserId.HasValue)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Usuário admin padrão criado: {Email}", adminEmail);

        // Criar projeto padrão "Heimdall" se necessário
        var projectId = await projectService.CreateProjectAsync(
            new CreateProjectRequest("Heimdall", "heimdall-api"),
            CancellationToken.None);

        if (projectId.HasValue)
        {
            // Associar admin ao projeto
            var userRepo = scope.ServiceProvider.GetRequiredService<Heimdall.Domain.Interfaces.IUserRepository>();
            var projectRepo = scope.ServiceProvider.GetRequiredService<Heimdall.Domain.Interfaces.IProjectRepository>();

            var user = await userRepo.GetByIdAsync(adminUserId.Value, CancellationToken.None);
            var project = await projectRepo.GetByIdAsync(projectId.Value, CancellationToken.None);

            if (user is not null && project is not null)
            {
                user.UserProjects.Add(new Heimdall.Domain.Entities.UserProject
                {
                    UserId = user.Id,
                    ProjectId = project.Id,
                    Role = "admin"
                });
                await userRepo.SaveChangesAsync(CancellationToken.None);
                logger.LogInformation("Usuário admin associado ao projeto Heimdall com role 'admin'");
            }
        }
    }
}

// ──────────────────────────────── Endpoints ────────────────────────────────


// Endpoint de login usando serviço real
app.MapPost("/api/login", async (LoginRequest request, IAuthService auth, HttpContext ctx, CancellationToken ct) =>
{
    var userAgent = ctx.Request.Headers.UserAgent.ToString();
    var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    var result = await auth.LoginAsync(request, userAgent, ip, ct);
    return result is null
        ? Results.Unauthorized()
        : Results.Ok(result);
})
.RequireRateLimiting("login")
.WithName("Login");

app.MapPost("/api/refresh", async (RefreshRequest request, IAuthService auth, HttpContext ctx, CancellationToken ct) =>
{
    var userAgent = ctx.Request.Headers.UserAgent.ToString();
    var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    var result = await auth.RefreshAsync(request, userAgent, ip, ct);
    return result is null
        ? Results.Unauthorized()
        : Results.Ok(result);
})
.WithName("Refresh");

app.MapPost("/api/revoke", async (RevokeRequest request, IAuthService auth, CancellationToken ct) =>
{
    var revoked = await auth.RevokeAsync(request, ct);
    return revoked ? Results.Ok() : Results.NotFound();
})
.RequireAuthorization()
.WithName("Revoke");

app.MapPost("/api/projects", async (CreateProjectRequest request, ProjectService projects, CancellationToken ct) =>
{
    var id = await projects.CreateProjectAsync(request, ct);
    return id is null
        ? Results.Conflict("A project with this audience already exists.")
        : Results.Created($"/api/projects/{id}", new { id });
})
.RequireAuthorization(p => p.RequireRole("admin"))
.WithName("CreateProject");

app.MapPost("/api/users", async (CreateUserRequest request, UserService users, CancellationToken ct) =>
{
    var id = await users.CreateUserAsync(request, ct);
    return id is null
        ? Results.Conflict("A user with this email already exists.")
        : Results.Created($"/api/users/{id}", new { id });
})
.RequireAuthorization(p => p.RequireRole("admin"))
.WithName("CreateUser");

// ──────────────────────────────── Fallback para Blazor ────────────────────────────────
// Mapeia todas as rotas não encontradas para o index.html do Blazor
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }
