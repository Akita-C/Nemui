using MessagePack;
using MessagePack.Resolvers;
using Nemui.Api.Extensions.DrawGameExtensions;

namespace Nemui.Api.Extensions;

public static class SignalrExtensions
{
    public static IServiceCollection AddSignalr(this IServiceCollection services)
    {
        services.AddSignalR()
            .AddMessagePackProtocol(options =>
            {
                options.SerializerOptions = MessagePackSerializerOptions.Standard
                    .WithResolver(CompositeResolver.Create(
                        DynamicEnumAsStringResolver.Instance,
                        StandardResolver.Instance
                    ));
            });
        services.AddDrawGame();
        return services;
    }

    public static WebApplication UseSignalr(this WebApplication app)
    {
        app.UseDrawGame();
        return app;
    }
}