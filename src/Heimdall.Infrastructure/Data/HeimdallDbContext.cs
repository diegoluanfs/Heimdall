using Heimdall.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Heimdall.Infrastructure.Data;

public class HeimdallDbContext : DbContext
{
    public HeimdallDbContext(DbContextOptions<HeimdallDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<UserProject> UserProjects => Set<UserProject>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).IsRequired().HasMaxLength(256);
            e.Property(u => u.PasswordHash).IsRequired();
        });

        modelBuilder.Entity<Project>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.Audience).IsUnique();
            e.Property(p => p.Name).IsRequired().HasMaxLength(100);
            e.Property(p => p.Audience).IsRequired().HasMaxLength(256);
        });

        modelBuilder.Entity<UserProject>(e =>
        {
            e.HasKey(up => new { up.UserId, up.ProjectId });
            e.HasOne(up => up.User).WithMany(u => u.UserProjects).HasForeignKey(up => up.UserId);
            e.HasOne(up => up.Project).WithMany(p => p.UserProjects).HasForeignKey(up => up.ProjectId);
            e.Property(up => up.Role).IsRequired().HasMaxLength(50);
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasKey(rt => rt.Id);
            e.HasIndex(rt => rt.RefreshTokenHash);
            e.HasOne(rt => rt.User).WithMany(u => u.RefreshTokens).HasForeignKey(rt => rt.UserId);
            e.Property(rt => rt.ProjectId).IsRequired();
            e.Property(rt => rt.RefreshTokenHash).IsRequired();
            e.Property(rt => rt.UserAgent).HasMaxLength(512);
            e.Property(rt => rt.Ip).HasMaxLength(45);
        });
    }
}
