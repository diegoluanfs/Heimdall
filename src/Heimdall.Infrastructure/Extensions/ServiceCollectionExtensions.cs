using Heimdall.Application.Interfaces;
using Heimdall.Application.Services;
using Heimdall.Domain.Interfaces;
using Heimdall.Infrastructure.Data;
using Heimdall.Infrastructure.Repositories;
using Heimdall.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Heimdall.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=heimdall.db";

        services.AddDbContext<HeimdallDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUserProjectRepository, UserProjectRepository>();

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenService, RsaJwtTokenService>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<UserService>();
        services.AddScoped<ProjectService>();

        return services;
    }
}
