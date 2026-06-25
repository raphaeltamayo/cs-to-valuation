using CStoValuation.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CStoValuation.Tests.TestSupport;

/// <summary>
/// An <see cref="IDbContextFactory{TContext}"/> backed by a private in-memory SQLite
/// database. Using the real SQLite provider (rather than EF's in-memory provider) means
/// the tests exercise actual SQL and the real schema. The single connection is held open
/// for the fixture's lifetime, because an in-memory SQLite database vanishes the moment
/// its last connection closes.
/// </summary>
internal sealed class SqliteInMemoryContextFactory : IDbContextFactory<AppDbContext>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public SqliteInMemoryContextFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new AppDbContext(_options);
        context.Database.EnsureCreated();
    }

    public AppDbContext CreateDbContext() => new(_options);

    public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(CreateDbContext());

    public void Dispose() => _connection.Dispose();
}
