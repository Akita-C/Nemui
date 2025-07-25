using System.Text.Json;
using Microsoft.AspNetCore.HttpOverrides;
using Nemui.Api.Extensions;
using Nemui.Api.Middlewares;
using Nemui.Infrastructure.Extensions;
using Serilog;

// Configure logging
LoggingExtensions.ConfigureLogging();

try
{
    Log.Information("Starting Nemui API application");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                                   ForwardedHeaders.XForwardedProto |
                                   ForwardedHeaders.XForwardedHost;

        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();

        options.ForwardLimit = null;
    });

    builder.Services.AddControllers().AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });
    builder.Services.AddDatabase(builder.Configuration);
    builder.Services.AddConfigurations(builder.Configuration);
    builder.Services.AddCaching(builder.Configuration);
    builder.Services.AddCustomApiVersioning();
    builder.Services.AddCustomAuthentication(builder.Configuration);
    builder.Services.AddApplicationServices();
    builder.Services.AddCustomSwagger();
    builder.Services.AddCustomCors(builder.Configuration);
    builder.Services.AddCustomRateLimiting();
    builder.Services.AddSeeders();
    builder.Services.AddSignalr();
    builder.Services.AddAIServices();

    var app = builder.Build();
    app.UseForwardedHeaders();
    app.UseCustomLogging();

    if (app.Environment.IsDevelopment())
    {
        app.MapSeedEndpoints();
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Nemui API v1");
            c.RoutePrefix = string.Empty; // Serve Swagger at root
        });
    }

    app.UseHttpsRedirection();
    app.UseCors("AllowSpecificOrigins");
    app.UseRateLimiter();
    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseMiddleware<JwtMiddleware>();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.UseSignalr();

    Log.Information("Nemui API application started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
