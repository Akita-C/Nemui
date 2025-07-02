using Nemui.Api.Hubs.Draw;
using Nemui.Application.Services.Games.Draw;
using Nemui.Infrastructure.Services.Games.Draw;

namespace Nemui.Api.Extensions.DrawGameExtensions;

public static class DrawGameExtension
{
    public static IServiceCollection AddDrawGame(this IServiceCollection services)
    {
        services.AddScoped<IDrawGameService, RedisDrawGameService>();
        services.AddSingleton<IRoundTimerService, RoundTimerService>();

        return services;
    }

    public static WebApplication UseDrawGame(this WebApplication app)
    {
        app.MapHub<DrawGameHub>("/hubs/draw-game");
        return app;
    }
}