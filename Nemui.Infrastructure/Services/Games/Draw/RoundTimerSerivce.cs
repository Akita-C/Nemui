using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Nemui.Application.Services.Games.Draw;
using Nemui.Shared.DTOs.Games.Draw;
using Nemui.Shared.Enums;

namespace Nemui.Infrastructure.Services.Games.Draw;

public class RoundTimerService(
    IDrawGameService gameService,
    IWordRevealService wordRevealService,
    ILogger<RoundTimerService> logger
) : IRoundTimerService
{
    private readonly ConcurrentDictionary<Guid, RoundTimer> activeRounds = new();
    private bool isDisposed = false;

    public event Func<RoundStartedEvent, Task>? OnRoundStarted;
    public event Func<EndedGameEvent, Task>? OnEndedGame;
    public event Func<PhaseChangedEvent, Task>? OnPhaseChanged;
    public event Func<WordRevealedEvent, Task>? OnWordRevealed;

    public async Task StartRoundAsync(Guid roomId, int totalRounds, DrawRoomConfig config)
    {
        if (isDisposed) return;

        // Stop any existing round for this room
        await StopRoundAsync(roomId);

        var roundTimer = new RoundTimer(
            roomId,
            totalRounds,
            config,
            OnTimerElapsed,
            OnPhaseElapsed,
            OnWordRevealedElapsed,
            gameService,
            wordRevealService
        );

        if (activeRounds.TryAdd(roomId, roundTimer))
        {
            var (currentDrawerId, word, roundNumber) = await gameService.StartNextRoundAsync(roomId);
            if (currentDrawerId == null || word == null)
            {
                logger.LogError("There must be something wrong for it to reach this case");
                return;
            }

            await gameService.SetRoundStartTimeAsync(roomId, DateTimeOffset.UtcNow);

            roundTimer.Start();

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
        }
    }

    public Task StopRoundAsync(Guid roomId)
    {
        if (activeRounds.TryRemove(roomId, out var roundTimer))
        {
            roundTimer.Dispose();
        }

        return Task.CompletedTask;
    }

    public async Task ForceRevealPhaseAsync(Guid roomId)
    {
        if (activeRounds.TryGetValue(roomId, out var roundTimer))
        {
            await roundTimer.ForceRevealPhaseAsync();
        }
    }

    public Task<bool> IsRoundActiveAsync(Guid roomId)
    {
        return Task.FromResult(activeRounds.ContainsKey(roomId));
    }

    public async Task<int?> GetRemainingTimeAsync(Guid roomId)
    {
        if (activeRounds.TryGetValue(roomId, out var roundTimer))
        {
            return await roundTimer.GetRemainingSecondsAsync();
        }

        return null;
    }

    private async Task OnTimerElapsed(Guid roomId)
    {
        if (activeRounds.TryRemove(roomId, out var roundTimer))
        {
            roundTimer.Dispose();

            var endEvent = new EndedGameEvent
            {
                RoomId = roomId
            };

            if (OnEndedGame != null) await OnEndedGame(endEvent);
        }
    }

    private async Task OnPhaseElapsed(PhaseChangedEvent phaseEvent)
    {
        if (OnPhaseChanged != null) await OnPhaseChanged(phaseEvent);
    }

    private async Task OnWordRevealedElapsed(WordRevealedEvent wordRevealedEvent)
    {
        if (OnWordRevealed != null) await OnWordRevealed(wordRevealedEvent);
    }

    public void Dispose()
    {
        if (isDisposed) return;

        foreach (var kvp in activeRounds)
        {
            kvp.Value.Dispose();
        }

        activeRounds.Clear();
        isDisposed = true;
    }

    private class RoundTimer(
        Guid roomId,
        int totalRounds,
        DrawRoomConfig config,
        Func<Guid, Task> onEndedGame,
        Func<PhaseChangedEvent, Task> onPhaseElapsed,
        Func<WordRevealedEvent, Task> onWordRevealed,
        IDrawGameService gameService,
        IWordRevealService wordRevealService
        ) : IDisposable
    {
        private readonly Guid roomId = roomId;
        private readonly int totalRounds = totalRounds;
        private readonly DrawRoomConfig config = config;
        private readonly Func<Guid, Task> onEndedGame = onEndedGame;
        private readonly Func<PhaseChangedEvent, Task> onPhaseElapsed = onPhaseElapsed;
        private readonly Func<WordRevealedEvent, Task> onWordRevealed = onWordRevealed;
        private readonly IDrawGameService gameService = gameService;
        private readonly IWordRevealService wordRevealService = wordRevealService;


        // ============== Phase timers ==============
        private PeriodicTimer? phaseTimer;
        private CancellationTokenSource? phaseCts;
        private DateTimeOffset phaseStartTime;

        // ============== Word reveal timer ==============
        private PeriodicTimer? wordRevealTimer;
        private CancellationTokenSource? wordRevealCts;
        private DateTimeOffset wordRevealStartTime;
        private Task? wordRevealTask;


        public async Task<int> GetRemainingSecondsAsync()
        {
            var elapsed = (int)(DateTimeOffset.UtcNow - phaseStartTime).TotalSeconds;
            var phaseDuration = GetPhaseDuration(await gameService.GetCurrentPhaseAsync(roomId));
            return Math.Max(0, phaseDuration - elapsed);
        }

        public async Task ForceRevealPhaseAsync()
        {
            CleanUpWordRevealTimers();
            CleanUpPhaseTimers();

            var basePhaseEvent = new PhaseChangedEvent
            {
                RoomId = roomId,
                Phase = DrawGamePhase.Reveal,
                DurationSeconds = GetPhaseDuration(DrawGamePhase.Reveal),
                StartTime = DateTimeOffset.UtcNow,
            };

            var phaseChangedEvent = await CreateAndHandleRevealPhaseChangedEvent(basePhaseEvent, await gameService.GetCurrentRoundAsync(roomId) ?? 0);

            await onPhaseElapsed(phaseChangedEvent);
            await StartPhaseAsync(DrawGamePhase.Reveal);
        }

        public void Start()
        {
            _ = StartPhaseAsync(DrawGamePhase.Drawing);
        }

        private async Task StartPhaseAsync(DrawGamePhase phase)
        {
            // Clean up previous phase timers
            CleanUpPhaseTimers();
            phaseCts = new CancellationTokenSource();
            phaseStartTime = DateTimeOffset.UtcNow;
            var duration = GetPhaseDuration(phase);
            phaseTimer = new PeriodicTimer(TimeSpan.FromSeconds(duration));

            if (phase == DrawGamePhase.Drawing && config.EnableWordReveal)
                StartWordRevealTimer();

            try
            {
                while (await phaseTimer.WaitForNextTickAsync(phaseCts.Token))
                {
                    await HandlePhaseAsync(phase);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Object reference not set to an instance of an object. in StartPhaseAsync");
                Console.WriteLine(ex.Message);
            }
        }

        private int GetPhaseDuration(DrawGamePhase phase) => phase switch
        {
            DrawGamePhase.Drawing => config.DrawingDurationSeconds,
            DrawGamePhase.Guessing => config.GuessingDurationSeconds,
            DrawGamePhase.Reveal => config.RevealDurationSeconds,
            _ => throw new ArgumentException("Invalid phase")
        };

        private void StartWordRevealTimer()
        {
            CleanUpWordRevealTimers();
            wordRevealCts = new CancellationTokenSource();
            wordRevealStartTime = DateTimeOffset.UtcNow;

            wordRevealTimer = new PeriodicTimer(TimeSpan.FromSeconds(config!.WordRevealIntervalSeconds));

            wordRevealTask = Task.Run(async () =>
            {
                try
                {
                    // Process the word reveal immediately
                    await ProcessWordRevealAsync();

                    // Process the word reveal every interval
                    while (await wordRevealTimer.WaitForNextTickAsync(wordRevealCts.Token))
                    {
                        await ProcessWordRevealAsync();
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Word reveal timer canceled");
                }
                catch (NullReferenceException)
                {
                    Console.WriteLine("Word reveal timer stopped due to cleanup - this is expected");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error in word reveal timer: " + ex.Message);
                }
            });
        }

        private async Task ProcessWordRevealAsync()
        {
            try
            {
                var currentWord = await gameService.GetCurrentWordAsync(roomId);
                if (currentWord == null)
                {
                    Console.WriteLine("ProcessWordRevealAsync | There must be something wrong for it to reach this case ");
                    return;
                }

                var elapsedSeconds = (int)(DateTimeOffset.UtcNow - wordRevealStartTime).TotalSeconds;
                var totalDrawingSeconds = config.DrawingDurationSeconds;
                var revealPercentage = Math.Min(
                    (float)elapsedSeconds / totalDrawingSeconds * config.MaxWordRevealPercentage,
                    config.MaxWordRevealPercentage
                );

                var revealedWord = wordRevealService.RevealWord(currentWord, revealPercentage);

                var wordRevealedEvent = new WordRevealedEvent
                {
                    RoomId = roomId,
                    RevealedWord = revealedWord,
                };

                await onWordRevealed(wordRevealedEvent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error processing word reveal: " + ex.Message);
            }
        }

        private async Task HandlePhaseAsync(DrawGamePhase phase)
        {
            var roundNumber = await gameService.GetCurrentRoundAsync(roomId) ?? 0;
            var nextPhase = GetNextPhase(phase, roundNumber);

            if (!nextPhase.HasValue)
            {
                await onEndedGame(roomId);
                return;
            }

            var basePhaseEvent = new PhaseChangedEvent
            {
                RoomId = roomId,
                Phase = nextPhase.Value,
                DurationSeconds = GetPhaseDuration(nextPhase.Value),
                StartTime = DateTimeOffset.UtcNow,
            };

            var phaseChangedEvent = nextPhase.Value switch
            {
                DrawGamePhase.Drawing => await CreateAndHandleNextRoundChangedEvent(basePhaseEvent),
                DrawGamePhase.Reveal => await CreateAndHandleRevealPhaseChangedEvent(basePhaseEvent, roundNumber),
                _ => await CreateAndHandleDefaultPhaseChangedEvent(basePhaseEvent, roundNumber)
            };

            await onPhaseElapsed(phaseChangedEvent);
            await StartPhaseAsync(nextPhase.Value);
        }

        private async Task<PhaseChangedEvent> CreateAndHandleNextRoundChangedEvent(PhaseChangedEvent basePhaseEvent)
        {
            var (currentDrawerId, word, nextRoundNumber) = await gameService.StartNextRoundAsync(roomId);
            if (currentDrawerId == null || word == null)
            {
                Console.WriteLine("CreateAndHandleNextRoundChangedEvent | There must be something wrong for it to reach this case");
            }
            await gameService.ResetAllPlayerHeartsAsync(roomId);

            await gameService.SetRoundStartTimeAsync(roomId, DateTimeOffset.UtcNow);

            return basePhaseEvent with
            {
                RoundNumber = nextRoundNumber,
                CurrentWord = word,
                CurrentDrawerId = currentDrawerId
            };
        }

        private async Task<PhaseChangedEvent> CreateAndHandleRevealPhaseChangedEvent(PhaseChangedEvent basePhaseEvent, int roundNumber)
        {
            await gameService.UpdateGamePhaseAsync(roomId, DrawGamePhase.Reveal);
            var fullWord = await gameService.GetCurrentWordAsync(roomId);
            if (fullWord == null)
            {
                Console.WriteLine("CreateAndHandleRevealPhaseChangedEvent | There must be something wrong for it to reach this case");
                return basePhaseEvent;
            }

            return basePhaseEvent with
            {
                RoundNumber = roundNumber,
                CurrentWord = fullWord
            };
        }

        private async Task<PhaseChangedEvent> CreateAndHandleDefaultPhaseChangedEvent(PhaseChangedEvent basePhaseEvent, int roundNumber)
        {
            await gameService.UpdateGamePhaseAsync(roomId, basePhaseEvent.Phase);

            if (basePhaseEvent.Phase == DrawGamePhase.Guessing) CleanUpWordRevealTimers();

            return basePhaseEvent with
            {
                RoundNumber = roundNumber,
            };
        }

        private DrawGamePhase? GetNextPhase(DrawGamePhase currentPhase, int roundNumber) => currentPhase switch
        {
            DrawGamePhase.Drawing => DrawGamePhase.Guessing,
            DrawGamePhase.Guessing => DrawGamePhase.Reveal,
            DrawGamePhase.Reveal => roundNumber < totalRounds ? DrawGamePhase.Drawing : null,
            _ => throw new ArgumentException("Invalid phase")
        };

        private void CleanUpPhaseTimers()
        {
            try
            {
                phaseCts?.Cancel();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error cleaning up phase timers: " + ex.Message);
            }
            finally
            {
                phaseCts?.Dispose();
                phaseTimer?.Dispose();

                phaseCts = null;
                phaseTimer = null;
            }
        }

        private void CleanUpWordRevealTimers()
        {
            try
            {
                wordRevealCts?.Cancel();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error cleaning up word reveal timers: " + ex.Message);
            }
            finally
            {
                wordRevealCts?.Dispose();
                wordRevealTimer?.Dispose();

                wordRevealCts = null;
                wordRevealTimer = null;
                wordRevealTask = null;
            }
        }

        public void Dispose()
        {
            CleanUpPhaseTimers();
            CleanUpWordRevealTimers();
        }
    }
}