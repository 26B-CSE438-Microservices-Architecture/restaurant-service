using Microsoft.EntityFrameworkCore;
using RestaurantService.API.Data;

namespace RestaurantService.Tests.Helpers;

/// <summary>
/// InMemory veritabanı ile test için AppDbContext oluşturur.
/// Her test için izole bir veritabanı sağlar.
/// </summary>
public static class TestDbContextFactory
{
    public static AppDbContext Create(string? dbName = null)
    {
        dbName ??= Guid.NewGuid().ToString();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
