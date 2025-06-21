using Nemui.Api.Extensions.DrawGameExtensions;

namespace Nemui.Api.Extensions;

public static class SignalrExtensions
{
    public static IServiceCollection AddSignalr(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddDrawGame();
        return services;
    }

    public static WebApplication UseSignalr(this WebApplication app)
    {
        app.UseDrawGame();
        return app;
    }
}