using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nemui.Application.Services.Games.Draw;
using Nemui.Infrastructure.Configurations;
using Nemui.Shared.DTOs.Games.Draw;
using Nemui.Shared.Enums;

namespace Nemui.Infrastructure.Services.Games.Draw;

public class RoundTimerService : IRoundTimerService
{
    private readonly ConcurrentDictionary<Guid, GameRoundManager> activeRounds = new();
    private readonly PeriodicTimer? cleanupTimer;
    private readonly Task? cleanupTask;
    private readonly TimeSpan cleanupInterval;
    private readonly TimeSpan finishedRoundTimeout;

    private readonly IDrawGameService gameService;
    private readonly IWordRevealService wordRevealService;
    private readonly ILogger<RoundTimerService> logger;


    private bool isDisposed = false;

    public event Func<RoundStartedEvent, Task>? OnRoundStarted;
    public event Func<EndedGameEvent, Task>? OnEndedGame;
    public event Func<PhaseChangedEvent, Task>? OnPhaseChanged;
    public event Func<WordRevealedEvent, Task>? OnWordRevealed;

    public RoundTimerService(
        IDrawGameService gameService,
        IWordRevealService wordRevealService,
        ILogger<RoundTimerService> logger,
        IOptions<RoundTimerSettings> roundTimerSettings
    )
    {
        this.gameService = gameService;
        this.wordRevealService = wordRevealService;
        this.logger = logger;

        cleanupInterval = roundTimerSettings.Value.CleanupInterval;
        finishedRoundTimeout = roundTimerSettings.Value.FinishedRoundTimeout;

        cleanupTimer = new PeriodicTimer(cleanupInterval);
        cleanupTask = StartCleanupTimerAsync();
    }

    public async Task StartRoundAsync(Guid roomId, int totalRounds, DrawRoomConfig config)
    {
        if (isDisposed)
        {
            logger.LogWarning("Cannot start round for room {RoomId} - service is disposed", roomId);
            return;
        }

        logger.LogDebug("Starting round for room {RoomId} with {TotalRounds} rounds", roomId, totalRounds);
        await StopRoundAsync(roomId);

        var gameRoundManager = new GameRoundManager(
            roomId,
            totalRounds,
            config,
            gameService,
            wordRevealService,
            logger
        );

        var eventHandlers = new GameEventHandlers
        {
            OnRoundStarted = async (@event) => { if (OnRoundStarted != null) await OnRoundStarted(@event); },
            OnEndedGame = async (@event) => { if (OnEndedGame != null) await OnEndedGame(@event); },
            OnPhaseChanged = async (@event) => { if (OnPhaseChanged != null) await OnPhaseChanged(@event); },
            OnWordRevealed = async (@event) => { if (OnWordRevealed != null) await OnWordRevealed(@event); }
        };

        gameRoundManager.OnRoundStarted += eventHandlers.OnRoundStarted;
        gameRoundManager.OnEndedGame += eventHandlers.OnEndedGame;
        gameRoundManager.OnPhaseChanged += eventHandlers.OnPhaseChanged;
        gameRoundManager.OnWordRevealed += eventHandlers.OnWordRevealed;

        gameRoundManager.SetEventHandlers(eventHandlers);

        if (activeRounds.TryAdd(roomId, gameRoundManager))
        {
            await gameRoundManager.StartAsync();
            logger.LogDebug("Successfully started round for room {RoomId}", roomId);
        }
        else
        {
            logger.LogWarning("Failed to start round for room {RoomId}", roomId);
            await gameRoundManager.StopAsync();
        }
    }

    public async Task StopRoundAsync(Guid roomId)
    {
        if (activeRounds.TryRemove(roomId, out var gameRoundManager))
        {
            logger.LogDebug("Stopping round for room {RoomId}", roomId);
            await gameRoundManager.StopAsync();
        }
    }

    public async Task ForceRevealPhaseAsync(Guid roomId)
    {
        if (activeRounds.TryGetValue(roomId, out var gameManager))
        {
            logger.LogInformation("Forcing reveal phase for room {RoomId}", roomId);
            await gameManager.ForceRevealPhaseAsync();
        }
        else
        {
            logger.LogWarning("Cannot force reveal phase - room {RoomId} not found", roomId);
        }
    }

    public Task<bool> IsRoundActiveAsync(Guid roomId)
    {
        return Task.FromResult(activeRounds.ContainsKey(roomId));
    }

    public async Task<int?> GetRemainingTimeAsync(Guid roomId)
    {
        if (activeRounds.TryGetValue(roomId, out var gameManager))
        {
            return await gameManager.GetRemainingSecondsAsync();
        }
        return null;
    }

    private async Task StartCleanupTimerAsync()
    {
        try
        {
            while (await cleanupTimer!.WaitForNextTickAsync())
            {
                if (isDisposed) break;

                try
                {
                    logger.LogDebug("Cleaning up finished rounds");
                    await CleanupFinishedRoundsAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error during cleanup finished rounds");
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogDebug("Cleanup task cancelled");
        }
    }

    private async Task CleanupFinishedRoundsAsync()
    {
        var finishedRooms = new List<Guid>();

        foreach (var kvp in activeRounds)
        {
            var gameManager = kvp.Value;
            if (gameManager.IsFinished &&
                gameManager.EndGameTimestamp.HasValue &&
                DateTimeOffset.UtcNow - gameManager.EndGameTimestamp.Value > finishedRoundTimeout)
            {
                finishedRooms.Add(kvp.Key);
            }
        }

        foreach (var roomId in finishedRooms)
        {
            if (activeRounds.TryRemove(roomId, out var gameManager))
            {
                await gameManager.StopAsync();
                gameManager.Dispose();
                logger.LogDebug("Cleaned up finished game round for room {RoomId}", roomId);
            }
        }

        if (finishedRooms.Count > 0)
        {
            logger.LogDebug("Cleaned up {Count} finished game rounds", finishedRooms.Count);
        }
    }

    public async void Dispose()
    {
        if (isDisposed) return;

        logger.LogDebug("Disposing RoundTimerService...");
        isDisposed = true;

        cleanupTimer?.Dispose();
        if (cleanupTask != null)
        {
            try
            {
                await cleanupTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        var stopTasks = activeRounds.Values.Select(manager => manager.StopAsync());
        await Task.WhenAll(stopTasks);

        activeRounds.Clear();
        logger.LogInformation("RoundTimerService disposed successfully");
    }
}

public class GameEventHandlers
{
    public Func<RoundStartedEvent, Task>? OnRoundStarted;
    public Func<EndedGameEvent, Task>? OnEndedGame;
    public Func<PhaseChangedEvent, Task>? OnPhaseChanged;
    public Func<WordRevealedEvent, Task>? OnWordRevealed;
}

public class GameRoundManager : IDisposable
{
    private readonly Guid roomId;
    private readonly int totalRounds;
    private readonly DrawRoomConfig config;
    private readonly IDrawGameService gameService;
    private readonly IWordRevealService wordRevealService;
    private readonly ILogger logger;

    // State Management - Thread-safe
    private readonly SemaphoreSlim stateLock = new(1, 1);
    private readonly CancellationTokenSource globalCts = new();
    private DrawGamePhase currentPhase = DrawGamePhase.Waiting;
    public DateTimeOffset? EndGameTimestamp { get; private set; }
    public bool IsFinished => currentPhase == DrawGamePhase.Finished;

    // Current Phase Management
    private PhaseRunner? currentPhaseRunner;
    private WordRevealRunner? wordRevealRunner;


    // Event Handlers
    private GameEventHandlers? eventHandlers;
    // Track for later unsubscribing
    private Func<DrawGamePhase, Task>? phaseCompletedHandler;
    private Func<WordRevealedEvent, Task>? wordRevealedHandler;
    // Event for outside world subscribe to
    public event Func<RoundStartedEvent, Task>? OnRoundStarted;
    public event Func<EndedGameEvent, Task>? OnEndedGame;
    public event Func<PhaseChangedEvent, Task>? OnPhaseChanged;
    public event Func<WordRevealedEvent, Task>? OnWordRevealed;

    public GameRoundManager(
        Guid roomId,
        int totalRounds,
        DrawRoomConfig config,
        IDrawGameService gameService,
        IWordRevealService wordRevealService,
        ILogger logger)
    {
        this.roomId = roomId;
        this.totalRounds = totalRounds;
        this.config = config;
        this.gameService = gameService;
        this.wordRevealService = wordRevealService;
        this.logger = logger;
    }

    public void SetEventHandlers(GameEventHandlers handlers) => eventHandlers = handlers;

    public async Task StartAsync()
    {
        await stateLock.WaitAsync(globalCts.Token);
        try
        {
            if (currentPhase != DrawGamePhase.Waiting)
            {
                logger.LogWarning("Cannot start game for room {RoomId} - already in phase {Phase}", roomId, currentPhase);
                return;
            }

            logger.LogDebug("Starting game for room {RoomId}", roomId);
            await StartFirstRoundAsync();
        }
        finally
        {
            stateLock.Release();
        }
    }

    public async Task StopAsync()
    {
        await stateLock.WaitAsync();
        try
        {
            if (currentPhase == DrawGamePhase.Finished)
            {
                logger.LogDebug("Game already finished for room {RoomId}", roomId);
                return;
            }

            logger.LogDebug("Stopping game for room {RoomId}", roomId);
            currentPhase = DrawGamePhase.Finished;
            globalCts.Cancel();

            await CleanupCurrentPhaseAsync();
            await CleanupWordRevealAsync();

            logger.LogDebug("Game for room {RoomId} stopped successfully", roomId);
        }
        finally
        {
            stateLock.Release();
        }
    }

    public async Task ForceRevealPhaseAsync()
    {
        await stateLock.WaitAsync(globalCts.Token);
        try
        {
            if (currentPhase == DrawGamePhase.Waiting || currentPhase == DrawGamePhase.Finished)
            {
                logger.LogWarning("Cannot force reveal phase for room {RoomId} - already in waiting or finished phase", roomId);
                return;
            }

            logger.LogDebug("Forcing reveal phase for room {RoomId}", roomId);
            await CleanupCurrentPhaseAsync();
            await CleanupWordRevealAsync();

            await StartPhaseAsync(DrawGamePhase.Reveal);
        }
        finally
        {
            stateLock.Release();
        }
    }

    public async Task<int> GetRemainingSecondsAsync()
    {
        await stateLock.WaitAsync(globalCts.Token);
        try
        {
            return currentPhaseRunner?.GetRemainingSeconds() ?? 0;
        }
        finally
        {
            stateLock.Release();
        }
    }

    private async Task StartFirstRoundAsync()
    {
        var (currentDrawerId, word, roundNumber) = await gameService.StartNextRoundAsync(roomId);
        if (currentDrawerId == null || word == null)
        {
            logger.LogError("Failed to start first round for room {RoomId} - no drawer or word", roomId);
            return;
        }

        await gameService.SetRoundStartTimeAsync(roomId, DateTimeOffset.UtcNow);

        var startEvent = new RoundStartedEvent
        {
            RoomId = roomId,
            RoundNumber = roundNumber,
            TotalRounds = totalRounds,
            DurationSeconds = config.DrawingDurationSeconds,
            StartTime = DateTimeOffset.UtcNow,
            CurrentDrawerId = currentDrawerId,
            CurrentWord = word
        };

        if (OnRoundStarted != null) await OnRoundStarted(startEvent);

        await StartPhaseAsync(DrawGamePhase.Drawing);
    }

    private async Task StartPhaseAsync(DrawGamePhase phase)
    {
        logger.LogDebug("Starting phase {Phase} for room {RoomId}", phase, roomId);
        currentPhase = phase;

        currentPhaseRunner = new PhaseRunner(
            roomId,
            phase,
            GetPhaseDuration(phase),
            globalCts.Token,
            logger
        );

        // Store the handler for later unsubscribing
        phaseCompletedHandler = OnPhaseCompletedAsync;
        currentPhaseRunner.OnPhaseCompleted += phaseCompletedHandler;

        if (phase == DrawGamePhase.Drawing && config.EnableWordReveal) await StartWordRevealAsync();

        var phaseEvent = await CreatePhaseChangedEventAsync(phase);
        if (OnPhaseChanged != null) await OnPhaseChanged(phaseEvent);

        await currentPhaseRunner.StartAsync();
    }

    private async Task StartWordRevealAsync()
    {
        logger.LogDebug("Starting word reveal for room {RoomId}", roomId);

        wordRevealRunner = new WordRevealRunner(
            roomId,
            config,
            gameService,
            wordRevealService,
            globalCts.Token,
            logger
        );

        // Store the handler for later unsubscribing
        wordRevealedHandler = async (@event) => { if (OnWordRevealed != null) await OnWordRevealed(@event); };
        wordRevealRunner.OnWordRevealed += wordRevealedHandler;

        await wordRevealRunner.StartAsync();
    }

    private async Task OnPhaseCompletedAsync(DrawGamePhase completedPhase)
    {
        await stateLock.WaitAsync(globalCts.Token);
        try
        {
            if (currentPhase == DrawGamePhase.Finished)
            {
                logger.LogDebug("Ignoring phase completion for room {RoomId} - game already finished", roomId);
                return;
            }

            logger.LogDebug("Phase {Phase} completed for room {RoomId}", completedPhase, roomId);

            if (completedPhase == DrawGamePhase.Guessing && wordRevealRunner != null)
                await CleanupWordRevealAsync();

            var roundNumber = await gameService.GetCurrentRoundAsync(roomId) ?? 0;
            var nextPhase = GetNextPhase(completedPhase, roundNumber);

            if (!nextPhase.HasValue)
            {
                logger.LogDebug("Game ended for room {RoomId}", roomId);
                EndGameTimestamp = DateTimeOffset.UtcNow;
                currentPhase = DrawGamePhase.Finished;
                await CleanupCurrentPhaseAsync();
                var endedEvent = new EndedGameEvent { RoomId = roomId };
                if (OnEndedGame != null) await OnEndedGame(endedEvent);
                return;
            }

            await HandlePhaseTransitionAsync(nextPhase.Value);
            await CleanupCurrentPhaseAsync();
            await StartPhaseAsync(nextPhase.Value);
        }
        finally
        {
            stateLock.Release();
        }
    }

    private async Task HandlePhaseTransitionAsync(DrawGamePhase nextPhase)
    {
        switch (nextPhase)
        {
            case DrawGamePhase.Drawing:
                logger.LogInformation("Transitioning to new drawing round for room {RoomId}", roomId);
                await gameService.StartNextRoundAsync(roomId);
                await gameService.ResetAllPlayerHeartsAsync(roomId);
                await gameService.SetRoundStartTimeAsync(roomId, DateTimeOffset.UtcNow);
                break;

            case DrawGamePhase.Reveal:
                logger.LogInformation("Transitioning to reveal phase for room {RoomId}", roomId);
                await gameService.UpdateGamePhaseAsync(roomId, DrawGamePhase.Reveal);
                break;

            case DrawGamePhase.Guessing:
                logger.LogInformation("Transitioning to guessing phase for room {RoomId}", roomId);
                await gameService.UpdateGamePhaseAsync(roomId, DrawGamePhase.Guessing);
                break;
        }
    }

    private async Task<PhaseChangedEvent> CreatePhaseChangedEventAsync(DrawGamePhase phase)
    {
        var baseEvent = new PhaseChangedEvent
        {
            RoomId = roomId,
            Phase = phase,
            DurationSeconds = GetPhaseDuration(phase),
            StartTime = DateTimeOffset.UtcNow,
        };

        return phase switch
        {
            DrawGamePhase.Drawing => baseEvent with
            {
                RoundNumber = await gameService.GetCurrentRoundAsync(roomId) ?? 0,
                CurrentWord = await gameService.GetCurrentWordAsync(roomId),
                CurrentDrawerId = await gameService.GetCurrentDrawerAsync(roomId),
            },
            DrawGamePhase.Reveal => baseEvent with
            {
                RoundNumber = await gameService.GetCurrentRoundAsync(roomId) ?? 0,
                CurrentWord = await gameService.GetCurrentWordAsync(roomId),
            },
            _ => baseEvent with
            {
                RoundNumber = await gameService.GetCurrentRoundAsync(roomId) ?? 0,
            }
        };
    }

    private DrawGamePhase? GetNextPhase(DrawGamePhase currentPhase, int roundNumber) => currentPhase switch
    {
        DrawGamePhase.Drawing => DrawGamePhase.Guessing,
        DrawGamePhase.Guessing => DrawGamePhase.Reveal,
        DrawGamePhase.Reveal => roundNumber < totalRounds ? DrawGamePhase.Drawing : null,
        _ => null
    };

    private int GetPhaseDuration(DrawGamePhase phase) => phase switch
    {
        DrawGamePhase.Drawing => config.DrawingDurationSeconds,
        DrawGamePhase.Guessing => config.GuessingDurationSeconds,
        DrawGamePhase.Reveal => config.RevealDurationSeconds,
        _ => throw new ArgumentException($"Invalid phase: {phase}")
    };

    private async Task CleanupCurrentPhaseAsync()
    {
        if (currentPhaseRunner != null)
        {
            if (phaseCompletedHandler != null)
            {
                currentPhaseRunner.OnPhaseCompleted -= phaseCompletedHandler;
                phaseCompletedHandler = null;
            }
            await currentPhaseRunner.StopAsync();
            currentPhaseRunner.Dispose();
            currentPhaseRunner = null;
        }
    }

    private async Task CleanupWordRevealAsync()
    {
        if (wordRevealRunner != null)
        {
            if (wordRevealedHandler != null)
            {
                wordRevealRunner.OnWordRevealed -= wordRevealedHandler;
                wordRevealedHandler = null;
            }
            await wordRevealRunner.StopAsync();
            wordRevealRunner.Dispose();
            wordRevealRunner = null;
        }
    }

    public void Dispose()
    {
        logger.LogDebug("Disposing GameRoundManager for room {RoomId}", roomId);

        Task.Run(async () =>
        {
            await StopAsync();

            if (currentPhaseRunner != null)
            {
                if (OnRoundStarted != null && eventHandlers != null && eventHandlers.OnRoundStarted != null)
                    OnRoundStarted -= eventHandlers.OnRoundStarted;

                if (OnEndedGame != null && eventHandlers != null && eventHandlers.OnEndedGame != null)
                    OnEndedGame -= eventHandlers.OnEndedGame;

                if (OnPhaseChanged != null && eventHandlers != null && eventHandlers.OnPhaseChanged != null)
                    OnPhaseChanged -= eventHandlers.OnPhaseChanged;

                if (OnWordRevealed != null && eventHandlers != null && eventHandlers.OnWordRevealed != null)
                    OnWordRevealed -= eventHandlers.OnWordRevealed;

                eventHandlers = null;
            }
        }).Wait(TimeSpan.FromSeconds(5));

        globalCts?.Dispose();
        stateLock?.Dispose();
    }
}

public class PhaseRunner : IDisposable
{
    private readonly Guid roomId;
    private readonly DrawGamePhase phase;
    private readonly int durationSeconds;
    private readonly CancellationToken cancellationToken;
    private readonly ILogger logger;
    private readonly DateTimeOffset startTime;

    private PeriodicTimer? timer;
    private CancellationTokenSource? phaseCts;
    private Task? runnerTask;

    public event Func<DrawGamePhase, Task>? OnPhaseCompleted;

    private readonly TaskCompletionSource<bool> completionSource = new();

    public PhaseRunner(Guid roomId, DrawGamePhase phase, int durationSeconds, CancellationToken cancellationToken, ILogger logger)
    {
        this.roomId = roomId;
        this.phase = phase;
        this.durationSeconds = durationSeconds;
        this.cancellationToken = cancellationToken;
        this.logger = logger;
        startTime = DateTimeOffset.UtcNow;
    }

    public async Task StartAsync()
    {
        logger.LogDebug("Starting phase runner for {Phase} in room {RoomId} with duration {DurationSeconds}", phase, roomId, durationSeconds);

        phaseCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timer = new PeriodicTimer(TimeSpan.FromSeconds(durationSeconds));

        // Timer task - signals completion without blocking
        runnerTask = Task.Run(async () =>
        {
            try
            {
                while (await timer.WaitForNextTickAsync(phaseCts.Token))
                {
                    logger.LogDebug("Phase {Phase} timer tick for room {RoomId}", phase, roomId);
                    // Signal completion without calling handler directly, doesnot await anything, so it's not a blocking call
                    completionSource.TrySetResult(true);
                    logger.LogDebug("Phase {Phase} completion signal set for room {RoomId}", phase, roomId);
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogDebug("Phase {Phase} cancelled for room {RoomId} in timer task", phase, roomId);
                completionSource.TrySetCanceled();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in phase runner for {Phase} in room {RoomId}", phase, roomId);
                completionSource.TrySetException(ex);
            }
        }, phaseCts.Token);

        // Completion handler - processes completion signal to avoid deadlock  
        _ = Task.Run(async () =>
        {
            try
            {
                var result = await completionSource.Task;
                if (OnPhaseCompleted != null) await OnPhaseCompleted(phase);
            }
            catch (OperationCanceledException)
            {
                logger.LogDebug("Phase {Phase} cancelled for room {RoomId} in completion handler", phase, roomId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in phase completion for {Phase} in room {RoomId}", phase, roomId);
            }
        });
    }

    public async Task StopAsync()
    {
        logger.LogDebug("Stopping phase runner for {Phase} in room {RoomId}", phase, roomId);

        phaseCts?.Cancel();
        completionSource.TrySetCanceled();
    }

    public int GetRemainingSeconds()
    {
        var elapsed = (int)(DateTimeOffset.UtcNow - startTime).TotalSeconds;
        return Math.Max(0, durationSeconds - elapsed);
    }

    public void Dispose()
    {
        OnPhaseCompleted = null;
        phaseCts?.Dispose();
        timer?.Dispose();
    }
}

public class WordRevealRunner : IDisposable
{
    private readonly Guid roomId;
    private readonly DrawRoomConfig config;
    private readonly IDrawGameService gameService;
    private readonly IWordRevealService wordRevealService;
    private readonly CancellationToken cancellationToken;
    private readonly ILogger logger;
    private readonly DateTimeOffset startTime;

    private PeriodicTimer? timer;
    private CancellationTokenSource? revealCts;
    private Task? revealTask;

    public event Func<WordRevealedEvent, Task>? OnWordRevealed;

    public WordRevealRunner(
        Guid roomId,
        DrawRoomConfig config,
        IDrawGameService gameService,
        IWordRevealService wordRevealService,
        CancellationToken cancellationToken,
        ILogger logger)
    {
        this.roomId = roomId;
        this.config = config;
        this.gameService = gameService;
        this.wordRevealService = wordRevealService;
        this.cancellationToken = cancellationToken;
        this.logger = logger;
        startTime = DateTimeOffset.UtcNow;
    }

    public async Task StartAsync()
    {
        revealCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timer = new PeriodicTimer(TimeSpan.FromSeconds(config.WordRevealIntervalSeconds));

        revealTask = Task.Run(async () =>
        {
            try
            {
                // Initial reveal
                await ProcessWordRevealAsync();

                // Periodic reveals
                while (await timer.WaitForNextTickAsync(revealCts.Token))
                {
                    await ProcessWordRevealAsync();
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogDebug("Word reveal cancelled for room {RoomId}", roomId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in word reveal for room {RoomId}", roomId);
            }
        }, revealCts.Token);
    }

    public async Task StopAsync()
    {
        logger.LogDebug("Stopping word reveal for room {RoomId}", roomId);

        revealCts?.Cancel();

        if (revealTask != null)
        {
            try
            {
                await revealTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error stopping word reveal for room {RoomId}", roomId);
            }
        }
    }

    private async Task ProcessWordRevealAsync()
    {
        try
        {
            var currentWord = await gameService.GetCurrentWordAsync(roomId);
            if (currentWord == null)
            {
                logger.LogWarning("No current word found for room {RoomId}", roomId);
                return;
            }

            var elapsedSeconds = (int)(DateTimeOffset.UtcNow - startTime).TotalSeconds;
            var revealPercentage = Math.Min(
                (float)elapsedSeconds / config.DrawingDurationSeconds * config.MaxWordRevealPercentage,
                config.MaxWordRevealPercentage
            );

            var revealedWord = wordRevealService.RevealWord(currentWord, revealPercentage);

            var wordRevealedEvent = new WordRevealedEvent
            {
                RoomId = roomId,
                RevealedWord = revealedWord,
            };

            if (OnWordRevealed != null) await OnWordRevealed(wordRevealedEvent);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing word reveal for room {RoomId}", roomId);
        }
    }

    public void Dispose()
    {
        OnWordRevealed = null;
        revealCts?.Dispose();
        timer?.Dispose();
    }
}