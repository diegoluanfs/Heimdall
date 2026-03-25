using System.Security.Cryptography;
using System.Threading.RateLimiting;
using Heimdall.Application.DTOs;
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


// Servir arquivos estáticos da pasta web na raiz do projeto
var webStaticPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "web");
if (Directory.Exists(webStaticPath))
{
    builder.Services.AddDirectoryBrowser();
}


// Serviço de autenticação mockado para testes
builder.Services.AddSingleton<IAuthService, MockAuthService>();

var app = builder.Build();

// ──────────────────────────────── Middleware pipeline ────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}


// Servir arquivos estáticos da pasta web em /web
if (Directory.Exists(webStaticPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(webStaticPath),
        RequestPath = "/web"
    });
    app.UseDirectoryBrowser(new DirectoryBrowserOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(webStaticPath),
        RequestPath = "/web"
    });
}

app.UseHttpsRedirection();

// Security headers
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    ctx.Response.Headers.Append("X-Frame-Options", "DENY");
    ctx.Response.Headers.Append("X-XSS-Protection", "0");
    ctx.Response.Headers.Append("Referrer-Policy", "no-referrer");
    ctx.Response.Headers.Append("Content-Security-Policy", "default-src 'none'");
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
}

// ──────────────────────────────── Endpoints ────────────────────────────────


// Endpoint de login usando serviço mockado
app.MapPost("/login", async (LoginRequest request, IAuthService auth, HttpContext ctx, CancellationToken ct) =>
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

app.MapPost("/refresh", async (RefreshRequest request, AuthService auth, HttpContext ctx, CancellationToken ct) =>
{
    var userAgent = ctx.Request.Headers.UserAgent.ToString();
    var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    var result = await auth.RefreshAsync(request, userAgent, ip, ct);
    return result is null
        ? Results.Unauthorized()
        : Results.Ok(result);
})
.WithName("Refresh");

app.MapPost("/revoke", async (RevokeRequest request, AuthService auth, CancellationToken ct) =>
{
    var revoked = await auth.RevokeAsync(request, ct);
    return revoked ? Results.Ok() : Results.NotFound();
})
.RequireAuthorization()
.WithName("Revoke");

app.MapPost("/projects", async (CreateProjectRequest request, ProjectService projects, CancellationToken ct) =>
{
    var id = await projects.CreateProjectAsync(request, ct);
    return id is null
        ? Results.Conflict("A project with this audience already exists.")
        : Results.Created($"/projects/{id}", new { id });
})
.RequireAuthorization(p => p.RequireRole("admin"))
.WithName("CreateProject");

app.MapPost("/users", async (CreateUserRequest request, UserService users, CancellationToken ct) =>
{
    var id = await users.CreateUserAsync(request, ct);
    return id is null
        ? Results.Conflict("A user with this email already exists.")
        : Results.Created($"/users/{id}", new { id });
})
.RequireAuthorization(p => p.RequireRole("admin"))
.WithName("CreateUser");


// Endpoint para servir a página de login.html na raiz
app.MapGet("/", async context =>
{
    var loggerFactory = context.RequestServices.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
    var logger = loggerFactory?.CreateLogger("RootEndpointLogger");
    // Permite definir o caminho do login.html por variável de ambiente ou appsettings
    var envFilePath = Environment.GetEnvironmentVariable("LOGIN_HTML_PATH");
    var configFilePath = context.RequestServices.GetService<IConfiguration>()?["LoginHtmlPath"];
    string filePath = envFilePath ?? configFilePath;
    if (string.IsNullOrWhiteSpace(filePath))
    {
        // Caminho relativo padrão (para desenvolvimento)
        var root = AppContext.BaseDirectory;
        var projectRoot = Path.GetFullPath(Path.Combine(root, "..", ".."));
        filePath = Path.Combine(projectRoot, "web", "login.html");
    }
    logger?.LogInformation($"[LOGIN SERVE] Caminho usado: {filePath} | Existe: {File.Exists(filePath)}");
    if (File.Exists(filePath))
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.SendFileAsync(filePath);
    }
    else
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteAsync($"Página de login não encontrada. Caminho: {filePath}");
    }
});

app.Run();

public partial class Program { }
