using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Heimdall.Infrastructure.Data;

public class HeimdallDbContextFactory : IDesignTimeDbContextFactory<HeimdallDbContext>
{
    public HeimdallDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<HeimdallDbContext>()
            .UseSqlite("Data Source=heimdall.db")
            .Options;
        return new HeimdallDbContext(options);
    }
}
