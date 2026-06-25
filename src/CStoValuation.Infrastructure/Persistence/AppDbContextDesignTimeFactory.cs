using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CStoValuation.Infrastructure.Persistence;

/// <summary>
/// Lets the <c>dotnet ef</c> tooling construct an <see cref="AppDbContext"/> at design time
/// (e.g. when adding a migration) without running the app's DI container. The connection
/// string here is only used to know the provider's SQL dialect while generating migrations;
/// the real database path is supplied at runtime by the host.
/// </summary>
internal sealed class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=design-time.db")
            .Options;

        return new AppDbContext(options);
    }
}
