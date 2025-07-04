using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Nemui.Application.Services.Games.Draw;
using Nemui.Shared.DTOs.Games.Draw;
using Nemui.Shared.Enums;

namespace Nemui.Infrastructure.Services.Games.Draw;

public class RoundTimerService(
    IDrawGameService gameService,
    ILogger<RoundTimerService> logger
) : IRoundTimerService
{
    private readonly ConcurrentDictionary<Guid, RoundTimer> activeRounds = new();
    private bool isDisposed = false;

    public event Func<RoundStartedEvent, Task>? OnRoundStarted;
    public event Func<EndedGameEvent, Task>? OnEndedGame;
    public event Func<PhaseChangedEvent, Task>? OnPhaseChanged;

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
            gameService
        );

        if (activeRounds.TryAdd(roomId, roundTimer))
        {
            var (currentDrawerId, word, roundNumber) = await gameService.StartNextRoundAsync(roomId);
            if (currentDrawerId == null || word == null)
            {
                logger.LogError("There must be something wrong for it to reach this case");
                return;
            }

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

    private class RoundTimer : IDisposable
    {
        private readonly Guid roomId;
        private readonly int totalRounds;
        private readonly DrawRoomConfig config;
        private readonly Func<Guid, Task> onEndedGame;
        private readonly Func<PhaseChangedEvent, Task> onPhaseElapsed;
        private readonly IDrawGameService gameService;

        private Timer? currentTimer;
        private DateTimeOffset phaseStartTime;


        public async Task<int> GetRemainingSecondsAsync()
        {
            var elapsed = (int)(DateTimeOffset.UtcNow - phaseStartTime).TotalSeconds;
            var phaseDuration = GetPhaseDuration(await gameService.GetCurrentPhaseAsync(roomId));
            return Math.Max(0, phaseDuration - elapsed);
        }

        public RoundTimer(
            Guid roomId,
            int totalRounds,
            DrawRoomConfig config,
            Func<Guid, Task> onEndedGame,
            Func<PhaseChangedEvent, Task> onPhaseElapsed,
            IDrawGameService gameService
        )
        {
            this.roomId = roomId;
            this.totalRounds = totalRounds;
            this.config = config;
            this.onEndedGame = onEndedGame;
            this.onPhaseElapsed = onPhaseElapsed;
            this.gameService = gameService;
        }

        public void Start()
        {
            StartPhase(DrawGamePhase.Drawing);
        }

        private void StartPhase(DrawGamePhase phase)
        {
            phaseStartTime = DateTimeOffset.UtcNow;
            var duration = GetPhaseDuration(phase);
            currentTimer = new Timer(OnPhaseCallback, phase, TimeSpan.FromSeconds(duration), Timeout.InfiniteTimeSpan);
        }

        private int GetPhaseDuration(DrawGamePhase phase) => phase switch
        {
            DrawGamePhase.Drawing => config.DrawingDurationSeconds,
            DrawGamePhase.Guessing => config.GuessingDurationSeconds,
            DrawGamePhase.Reveal => config.RevealDurationSeconds,
            _ => throw new ArgumentException("Invalid phase")
        };

        private async void OnPhaseCallback(object? state)
        {
            try
            {
                if (state is not DrawGamePhase completedPhase) return;

                var roundNumber = await gameService.GetCurrentRoundAsync(roomId) ?? 0;
                var nextPhase = GetNextPhase(completedPhase, roundNumber);

                // This means the game is over
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

                var phaseEvent = nextPhase switch
                {
                    DrawGamePhase.Reveal => await CreateNextPhaseChangedEvent(basePhaseEvent),
                    _ => await CreatePhaseChangedEvent(basePhaseEvent, roundNumber)
                };

                await onPhaseElapsed(phaseEvent);
                StartPhase(nextPhase.Value);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task<PhaseChangedEvent> CreateNextPhaseChangedEvent(PhaseChangedEvent basePhaseEvent)
        {
            var (currentDrawerId, word, nextRoundNumber) = await gameService.StartNextRoundAsync(roomId);
            if (currentDrawerId == null || word == null)
            {
                Console.WriteLine("There must be something wrong for it to reach this case");
            }

            return basePhaseEvent with
            {
                RoundNumber = nextRoundNumber,
                CurrentDrawerId = currentDrawerId,
                CurrentWord = word
            };
        }

        private async Task<PhaseChangedEvent> CreatePhaseChangedEvent(PhaseChangedEvent basePhaseEvent, int roundNumber)
        {
            await gameService.UpdateGamePhaseAsync(roomId, basePhaseEvent.Phase);

            return basePhaseEvent with
            {
                RoundNumber = roundNumber,
            };
        }

        private DrawGamePhase? GetNextPhase(DrawGamePhase currentPhase, int roundNumber) => currentPhase switch
        {
            DrawGamePhase.Drawing => DrawGamePhase.Guessing,
            DrawGamePhase.Guessing => DrawGamePhase.Reveal,
            DrawGamePhase.Reveal => roundNumber <= totalRounds ? DrawGamePhase.Drawing : null,
            _ => throw new ArgumentException("Invalid phase")
        };

        public void Dispose()
        {
            currentTimer?.Dispose();
        }
    }
}