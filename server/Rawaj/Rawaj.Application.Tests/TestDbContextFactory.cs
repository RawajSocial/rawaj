using Microsoft.EntityFrameworkCore;
using Rawaj.Persistence;

namespace Rawaj.Application.Tests;

public static class TestDbContextFactory
{
    public static AppDbContext Create() => Create(Guid.NewGuid().ToString());

    /// <summary>Two contexts created with the same name share the same underlying InMemory
    /// database — needed to simulate two concurrent requests loading, then racing to save, the
    /// same row (see the RowVersion concurrency-conflict tests).</summary>
    public static AppDbContext Create(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new AppDbContext(options);
    }
}
