using Nemui.Api.Hubs.Draw;
using Nemui.Api.Services;
using Nemui.Application.Services.Games.Draw;
using Nemui.Infrastructure.Services.Games.Draw;

namespace Nemui.Api.Extensions.DrawGameExtensions;

public static class DrawGameExtension
{
    public static IServiceCollection AddDrawGame(this IServiceCollection services)
    {
        services.AddSingleton<IDrawGameService, RedisDrawGameService>();
        services.AddSingleton<IRoundTimerService, RoundTimerService>();
        services.AddSingleton<IDrawGameNotificationService, SignalRDrawGameNotificationService>();
        services.AddSingleton<IWordRevealService, WordRevealService>();
        services.AddHostedService<DrawGameEventHostedService>();

        return services;
    }

    public static WebApplication UseDrawGame(this WebApplication app)
    {
        app.MapHub<DrawGameHub>("/hubs/draw-game");
        return app;
    }
}