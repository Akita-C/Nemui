using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nemui.Application.Services.Games.Draw;

namespace Nemui.Infrastructure.Services.Games.Draw;

public class DrawGameEventHostedService(
    IRoundTimerService roundTimerService,
    IDrawGameNotificationService drawGameNotificationService,
    ILogger<DrawGameEventHostedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting DrawGameEventHostedService");

        roundTimerService.OnRoundStarted += drawGameNotificationService.NotifyRoundStartedAsync;
        roundTimerService.OnPhaseChanged += drawGameNotificationService.NotifyPhaseChangedAsync;
        roundTimerService.OnEndedGame += drawGameNotificationService.NotifyRoundEndedAsync;

        logger.LogInformation("DrawGameEventHostedService started");
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping DrawGameEventHostedService");

        roundTimerService.OnRoundStarted -= drawGameNotificationService.NotifyRoundStartedAsync;
        roundTimerService.OnPhaseChanged -= drawGameNotificationService.NotifyPhaseChangedAsync;
        roundTimerService.OnEndedGame -= drawGameNotificationService.NotifyRoundEndedAsync;

        logger.LogInformation("DrawGameEventHostedService stopped");
        await Task.CompletedTask;
    }
}
