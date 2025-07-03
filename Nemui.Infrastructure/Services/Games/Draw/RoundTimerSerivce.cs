using System.Collections.Concurrent;
using Nemui.Application.Services.Games.Draw;
using Nemui.Shared.DTOs.Games.Draw;
using Nemui.Shared.Enums;

namespace Nemui.Infrastructure.Services.Games.Draw;

public class RoundTimerService : IRoundTimerService
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
            OnPhaseElapsed
        );

        if (activeRounds.TryAdd(roomId, roundTimer))
        {
            roundTimer.Start();

            var startEvent = new RoundStartedEvent
            {
                RoomId = roomId,
                RoundNumber = roundTimer.RoundNumber,
                TotalRounds = totalRounds,
                DurationSeconds = config.DrawingDurationSeconds,
                StartTime = DateTimeOffset.UtcNow
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

    public Task<bool> IsPhaseActiveAsync(Guid roomId, DrawGamePhase phase)
    {
        if (activeRounds.TryGetValue(roomId, out var roundTimer))
        {
            return Task.FromResult(roundTimer.CurrentPhase == phase);
        }

        return Task.FromResult(false);
    }

    public Task<int?> GetRemainingTimeAsync(Guid roomId)
    {
        if (activeRounds.TryGetValue(roomId, out var roundTimer))
        {
            return Task.FromResult<int?>(roundTimer.RemainingSeconds);
        }

        return Task.FromResult<int?>(null);
    }

    public Task<DrawGamePhase?> GetCurrentPhaseAsync(Guid roomId)
    {
        if (activeRounds.TryGetValue(roomId, out var roundTimer))
        {
            return Task.FromResult<DrawGamePhase?>(roundTimer.CurrentPhase);
        }

        return Task.FromResult<DrawGamePhase?>(null);
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
        private int roundNumber = 0;
        private readonly int totalRounds;
        private readonly DrawRoomConfig config;
        private readonly Func<Guid, Task> onEndedGame;
        private readonly Func<PhaseChangedEvent, Task> onPhaseElapsed;

        private Timer? currentTimer;
        private DrawGamePhase currentPhase = DrawGamePhase.Drawing;
        private DateTimeOffset phaseStartTime;

        public DrawGamePhase CurrentPhase => currentPhase;
        public int RoundNumber => roundNumber;

        public int RemainingSeconds
        {
            get
            {
                var elapsed = (int)(DateTimeOffset.UtcNow - phaseStartTime).TotalSeconds;
                var phaseDuration = GetPhaseDuration(currentPhase);
                return Math.Max(0, phaseDuration - elapsed);
            }
        }

        public RoundTimer(
            Guid roomId,
            int totalRounds,
            DrawRoomConfig config,
            Func<Guid, Task> onEndedGame,
            Func<PhaseChangedEvent, Task> onPhaseElapsed
        )
        {
            this.roomId = roomId;
            this.totalRounds = totalRounds;
            this.config = config;
            this.onEndedGame = onEndedGame;
            this.onPhaseElapsed = onPhaseElapsed;
        }

        public void Start()
        {
            roundNumber = 1;
            StartPhase(DrawGamePhase.Drawing);
        }

        private void StartPhase(DrawGamePhase phase)
        {
            currentPhase = phase;
            phaseStartTime = DateTimeOffset.UtcNow;

            var duration = GetPhaseDuration(phase);
            currentTimer = new Timer(OnPhaseCallback, currentPhase, TimeSpan.FromSeconds(duration), Timeout.InfiniteTimeSpan);
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

                var nextPhase = GetNextPhase(completedPhase);

                if (nextPhase.HasValue)
                {
                    if (nextPhase == DrawGamePhase.Reveal) roundNumber++;

                    // Transition to next phase
                    var phaseEvent = new PhaseChangedEvent
                    {
                        RoomId = roomId,
                        RoundNumber = roundNumber,
                        Phase = nextPhase.Value,
                        DurationSeconds = GetPhaseDuration(nextPhase.Value),
                        StartTime = DateTimeOffset.UtcNow
                    };

                    await onPhaseElapsed(phaseEvent);
                    StartPhase(nextPhase.Value);
                }
                else
                {
                    // Game completed
                    await onEndedGame(roomId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private DrawGamePhase? GetNextPhase(DrawGamePhase currentPhase) => currentPhase switch
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