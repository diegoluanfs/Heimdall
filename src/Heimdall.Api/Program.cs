using System.Security.Cryptography;
using System.Threading.RateLimiting;
using FluentValidation;
using Heimdall.Api.Filters;
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

builder.Services.AddCors(o =>
{
    o.AddDefaultPolicy(p =>
    {
        if (builder.Environment.IsDevelopment())
        {
            p.AllowAnyOrigin()
             .AllowAnyMethod()
             .AllowAnyHeader();
        }
        else
        {
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            if (allowedOrigins.Length > 0)
            {
                p.WithOrigins(allowedOrigins)
                 .AllowAnyMethod()
                 .AllowAnyHeader()
                 .AllowCredentials();
            }
            else
            {
                throw new InvalidOperationException("Cors:AllowedOrigins must be configured in production.");
            }
        }
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

    o.AddFixedWindowLimiter("refresh", opts =>
    {
        opts.PermitLimit = 20;
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

// ──────────────────────────────── Validation ────────────────────────────────
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequest>();

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

// ──────────────────────────────── Auto-migrate and seed (Development only) ────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HeimdallDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // Auto-migrate apenas em desenvolvimento
    // Aplicar migrações do banco de dados
    var autoMigrate = builder.Configuration.GetValue<bool>("Database:AutoMigrate", false);

    if (app.Environment.IsDevelopment() || autoMigrate)
    {
        db.Database.Migrate();
        logger.LogInformation("Database migrated successfully ({Environment})", 
            app.Environment.EnvironmentName);
    }
    else
    {
        // Em produção sem auto-migrate, verificar se há migrações pendentes e alertar
        var pendingMigrations = db.Database.GetPendingMigrations().ToList();
        if (pendingMigrations.Any())
        {
            logger.LogWarning("Pending migrations detected: {Migrations}. Please run migrations manually.", 
                string.Join(", ", pendingMigrations));
            throw new InvalidOperationException("Pending migrations detected. Run migrations manually in production or set Database:AutoMigrate=true.");
        }
        logger.LogInformation("Database is up to date (Production)");
    }

    // Criar usuário admin padrão se não existir
    var userService = scope.ServiceProvider.GetRequiredService<UserService>();
    var projectService = scope.ServiceProvider.GetRequiredService<ProjectService>();
    var userRepo = scope.ServiceProvider.GetRequiredService<Heimdall.Domain.Interfaces.IUserRepository>();

    var adminEmail = builder.Configuration["Seed:AdminEmail"] ?? "admin@heimdall.com";
    var adminPassword = builder.Configuration["Seed:AdminPassword"] ?? "Admin@123";

    if (string.IsNullOrEmpty(adminPassword) || adminPassword == "Admin@123")
    {
        logger.LogWarning("Using default admin password. Please set Seed:AdminPassword in production!");
    }

    // Verificar se admin já existe
    var existingAdmin = await userRepo.GetByEmailAsync(adminEmail, CancellationToken.None);

    if (existingAdmin is null)
    {
        // Criar usuário admin
        var adminUserId = await userService.CreateUserAsync(
            new CreateUserRequest(adminEmail, adminPassword), 
            CancellationToken.None);

        if (adminUserId.HasValue)
        {
            logger.LogInformation("Admin user created: {Email}", adminEmail);

            // Criar projeto padrão "Heimdall" se necessário
            var projectId = await projectService.CreateProjectAsync(
                new CreateProjectRequest("Heimdall", "heimdall-api"),
                CancellationToken.None);

            if (projectId.HasValue)
            {
                // Associar admin ao projeto
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
                    logger.LogInformation("Admin user associated with Heimdall project with role 'admin'");
                }
            }
            else
            {
                logger.LogInformation("Heimdall project already exists");
            }
        }
        else
        {
            logger.LogWarning("Failed to create admin user - user may already exist");
        }
    }
    else
    {
        logger.LogInformation("Admin user already exists: {Email}", adminEmail);
    }
}

// ──────────────────────────────── Endpoints ────────────────────────────────


// Endpoint de login usando serviço real
app.MapPost("/api/login", async (LoginRequest request, IAuthService auth, HttpContext ctx, CancellationToken ct) =>
{
    var userAgent = ctx.Request.Headers.UserAgent.ToString();
    if (userAgent.Length > 512) userAgent = userAgent[..512];

    var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    if (ip.Length > 45) ip = ip[..45];

    var result = await auth.LoginAsync(request, userAgent, ip, ct);
    return result is null
        ? Results.Unauthorized()
        : Results.Ok(result);
})
.AddEndpointFilter<ValidationFilter<LoginRequest>>()
.RequireRateLimiting("login")
.WithName("Login");

app.MapPost("/api/refresh", async (RefreshRequest request, IAuthService auth, HttpContext ctx, CancellationToken ct) =>
{
    var userAgent = ctx.Request.Headers.UserAgent.ToString();
    if (userAgent.Length > 512) userAgent = userAgent[..512];

    var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    if (ip.Length > 45) ip = ip[..45];

    var result = await auth.RefreshAsync(request, userAgent, ip, ct);
    return result is null
        ? Results.Unauthorized()
        : Results.Ok(result);
})
.AddEndpointFilter<ValidationFilter<RefreshRequest>>()
.RequireRateLimiting("refresh")
.WithName("Refresh");

app.MapPost("/api/revoke", async (RevokeRequest request, IAuthService auth, CancellationToken ct) =>
{
    var revoked = await auth.RevokeAsync(request, ct);
    return revoked ? Results.Ok() : Results.NotFound();
})
.AddEndpointFilter<ValidationFilter<RevokeRequest>>()
.RequireAuthorization()
.WithName("Revoke");

app.MapPost("/api/projects", async (CreateProjectRequest request, ProjectService projects, CancellationToken ct) =>
{
    var id = await projects.CreateProjectAsync(request, ct);
    return id is null
        ? Results.Conflict("A project with this audience already exists.")
        : Results.Created($"/api/projects/{id}", new { id });
})
.AddEndpointFilter<ValidationFilter<CreateProjectRequest>>()
.RequireAuthorization(p => p.RequireRole("admin"))
.WithName("CreateProject");

app.MapPost("/api/users", async (CreateUserRequest request, UserService users, CancellationToken ct) =>
{
    var id = await users.CreateUserAsync(request, ct);
    return id is null
        ? Results.Conflict("A user with this email already exists.")
        : Results.Created($"/api/users/{id}", new { id });
})
.AddEndpointFilter<ValidationFilter<CreateUserRequest>>()
.RequireAuthorization(p => p.RequireRole("admin"))
.WithName("CreateUser");

// ──────────────────────────────── Fallback para Blazor ────────────────────────────────
// Mapeia todas as rotas não encontradas para o index.html do Blazor
app.MapFallbackToFile("index.html");

// Health check endpoint for monitoring
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0",
    environment = builder.Environment.EnvironmentName
}))
.WithName("HealthCheck")
.AllowAnonymous();

app.Run();

public partial class Program { }
