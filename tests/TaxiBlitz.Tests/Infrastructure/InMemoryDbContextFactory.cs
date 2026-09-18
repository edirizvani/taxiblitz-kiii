using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaxiBlitz.Domain.Identity;
using TaxiBlitz.Persistence;

namespace TaxiBlitz.Tests.Infrastructure;

public static class InMemoryDbContextFactory
{
    public static AppDbContext Create(string? dbName = null, bool useSqlite = false)
    {
        if (useSqlite)
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;
            var ctx = new AppDbContext(options);
            ctx.Database.EnsureCreated();
            return ctx;
        }
        else
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
                .Options;
            var ctx = new AppDbContext(options);
            ctx.Database.EnsureCreated();
            return ctx;
        }
    }

    public static async Task<AppDbContext> CreateWithSeedAsync(Action<AppDbContext> seed, bool useSqlite = false)
    {
        var ctx = Create(useSqlite: useSqlite);
        seed(ctx);
        await ctx.SaveChangesAsync();
        return ctx;
    }
}
