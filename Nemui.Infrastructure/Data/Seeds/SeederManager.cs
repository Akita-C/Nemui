using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Nemui.Infrastructure.Data.Seeds;

public class SeederManager(IServiceProvider serviceProvider, ILogger<SeederManager> logger)
{
    public async Task SeedAllAsync()
    {
        logger.LogInformation("Starting database seeding process");

        var seeders = serviceProvider.GetServices<ISeeder>()
            .OrderBy(s => s.Order)
            .ToList();

        if (!seeders.Any())
        {
            logger.LogWarning("No seeders found");
            return;
        }

        logger.LogInformation("Found {Count} seeders to execute", seeders.Count);

        var semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);
        var tasks = new List<Task>();

        // Group seeders by order to ensure proper execution sequence
        var seederGroups = seeders.GroupBy(s => s.Order).OrderBy(g => g.Key);

        foreach (var group in seederGroups)
        {
            // Wait for previous group to complete before starting next group
            await Task.WhenAll(tasks);
            tasks.Clear();

            // Execute seeders in the same order group in parallel
            tasks.AddRange(group.Select(seeder => ExecuteSeederAsync(seeder, semaphore)));
        }

        // Wait for the last group to complete
        await Task.WhenAll(tasks);

        logger.LogInformation("Database seeding process completed successfully");
    }

    private async Task ExecuteSeederAsync(ISeeder seeder, SemaphoreSlim semaphore)
    {
        await semaphore.WaitAsync();
        try
        {
            logger.LogInformation("Executing seeder: {SeederName} (Order: {Order})", seeder.Name, seeder.Order);
            await seeder.SeedAsync();
            logger.LogInformation("Completed seeder: {SeederName}", seeder.Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to execute seeder: {SeederName}", seeder.Name);
            throw;
        }
        finally
        {
            semaphore.Release();
        }
    }
}