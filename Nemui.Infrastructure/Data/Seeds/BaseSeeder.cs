using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nemui.Infrastructure.Data.Context;

namespace Nemui.Infrastructure.Data.Seeds;

public abstract class BaseSeeder(AppDbContext context, ILogger<BaseSeeder> logger) : ISeeder
{
    public abstract Task SeedAsync();
    public abstract int Order { get; }
    public abstract string Name { get; }

    protected async Task<bool> HasDataAsync<T>() where T : class
        => await context.Set<T>().AnyAsync();

    protected async Task BulkInsertAsync<T>(IEnumerable<T> entities) where T : class
    {
        if (!entities.Any()) return;

        await context.Set<T>().AddRangeAsync(entities);
        await context.SaveChangesAsync();
    }

    protected async Task<T?> FindByIdAsync<T>(Guid id) where T : class
        => await context.Set<T>().FindAsync(id);

    protected void LogSeedingStart()
        => logger.LogInformation("Starting seeding for {SeederName}", Name);

    protected void LogSeedingComplete(int count)
        => logger.LogInformation("Completed seeding for {SeederName}. Inserted {Count} records", Name, count);

    protected void LogSeedingSkipped()
        => logger.LogInformation("Skipping seeding for {SeederName} - data already exists", Name);
}
