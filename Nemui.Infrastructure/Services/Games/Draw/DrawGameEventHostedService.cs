using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nemui.Application.Services.Games.Draw;

namespace Nemui.Infrastructure.Services.Games.Draw;

public class DrawGameEventHostedService : IHostedService
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<DrawGameEventHostedService> logger;
    private IServiceScope? scope;
    private IRoundTimerService? roundTimerService;
    private IDrawGameEventHandler? drawGameEventHandler;

    public DrawGameEventHostedService(
        IServiceProvider serviceProvider,
        ILogger<DrawGameEventHostedService> logger
    )
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting DrawGameEventHostedService");
        scope = serviceProvider.CreateScope();
        roundTimerService = scope.ServiceProvider.GetRequiredService<IRoundTimerService>();
        drawGameEventHandler = scope.ServiceProvider.GetRequiredService<IDrawGameEventHandler>();

        roundTimerService.OnRoundStarted += drawGameEventHandler.HandleRoundStartedAsync;
        roundTimerService.OnRoundEnded += drawGameEventHandler.HandleRoundEndedAsync;

        logger.LogInformation("DrawGameEventHostedService started");
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping DrawGameEventHostedService");
        if (roundTimerService != null && drawGameEventHandler != null)
        {
            roundTimerService.OnRoundStarted -= drawGameEventHandler.HandleRoundStartedAsync;
            roundTimerService.OnRoundEnded -= drawGameEventHandler.HandleRoundEndedAsync;
        }

        scope?.Dispose();
        await Task.CompletedTask;
    }
}
