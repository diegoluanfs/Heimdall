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
        // Determinar provider baseado em configuração
        var usePostgres = configuration.GetValue<bool>("Database:UsePostgreSQL", false);
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (usePostgres)
        {
            // PostgreSQL para produção (Render)
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "PostgreSQL configurado mas ConnectionStrings:DefaultConnection não encontrada. " +
                    "Configure DATABASE_URL no Render ou ConnectionStrings__DefaultConnection.");
            }

            services.AddDbContext<HeimdallDbContext>(options =>
                options.UseNpgsql(connectionString));
        }
        else
        {
            // SQLite para desenvolvimento local
            connectionString ??= "Data Source=heimdall.db";

            services.AddDbContext<HeimdallDbContext>(options =>
                options.UseSqlite(connectionString));
        }

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
