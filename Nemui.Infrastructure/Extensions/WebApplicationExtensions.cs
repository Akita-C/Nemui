using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nemui.Infrastructure.Configurations;
using Nemui.Infrastructure.Data.Seeds;

namespace Nemui.Infrastructure.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication MapSeedEndpoints(this WebApplication app)
    {
        var adminSettings = app.Services.GetRequiredService<IOptions<AdminSettings>>().Value;
        
        if (!adminSettings.EnableSeedEndpoints)
        {
            return app;
        }

        var seedGroup = app.MapGroup("/api/admin/seed")
            .WithTags("Admin - Database Seeding");

        seedGroup.MapPost("/all", async (HttpContext context, SeederManager seederManager) =>
        {
            // Check API Key
            if (!context.Request.Headers.TryGetValue("X-Admin-Key", out var apiKey) || 
                apiKey != adminSettings.SeedApiKey)
            {
                return Results.Unauthorized();
            }

            try
            {
                await seederManager.SeedAllAsync();
                return Results.Ok(new { 
                    message = "Database seeding completed successfully",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    title: "Seeding failed",
                    statusCode: 500
                );
            }
        })
        .WithName("SeedDatabase")
        .WithSummary("Seed database with sample data")
        .WithDescription("Run all database seeders to populate with sample data. Requires X-Admin-Key header.");

        return app;
    }
}