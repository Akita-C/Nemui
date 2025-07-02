using System.Collections.Concurrent;
using Nemui.Application.Services.Games.Draw;
using Nemui.Shared.DTOs.Games.Draw;

namespace Nemui.Infrastructure.Services.Games.Draw;

public class RoundTimerService : IRoundTimerService
{
    private readonly ConcurrentDictionary<Guid, RoundTimer> activeRounds = new();
    private bool isDisposed = false;

    public event Func<RoundStartedEvent, Task>? OnRoundStarted;
    public event Func<RoundEndedEvent, Task>? OnRoundEnded;

    public async Task StartRoundAsync(Guid roomId, int roundNumber, int totalRounds, int durationSeconds)
    {
        if (isDisposed) return;

        // Stop any existing round for this room
        await StopRoundAsync(roomId);

        var roundTimer = new RoundTimer(
            roomId,
            roundNumber,
            totalRounds,
            durationSeconds,
            OnTimerElapsed
        );

        if (activeRounds.TryAdd(roomId, roundTimer))
        {
            roundTimer.Start();

            var startEvent = new RoundStartedEvent
            {
                RoomId = roomId,
                RoundNumber = roundNumber,
                TotalRounds = totalRounds,
                DurationSeconds = durationSeconds,
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

    public Task<int?> GetRemainingTimeAsync(Guid roomId)
    {
        if (activeRounds.TryGetValue(roomId, out var roundTimer))
        {
            return Task.FromResult<int?>(roundTimer.RemainingSeconds);
        }

        return Task.FromResult<int?>(null);
    }

    private async Task OnTimerElapsed(Guid roomId, int roundNumber, int totalRounds)
    {
        if (activeRounds.TryGetValue(roomId, out var roundTimer))
        {
            roundTimer.Dispose();

            var endEvent = new RoundEndedEvent
            {
                RoomId = roomId,
                RoundNumber = roundNumber,
                IsGameFinished = roundNumber >= totalRounds
            };

            if (OnRoundEnded != null) await OnRoundEnded(endEvent);
        }
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
        private readonly Timer timer;
        private readonly Guid roomId;
        private readonly int roundNumber;
        private readonly int totalRounds;
        private readonly int durationSeconds;
        private readonly DateTimeOffset startTime;
        private readonly Func<Guid, int, int, Task> onElapsed;

        public RoundTimer(
            Guid roomId,
            int roundNumber,
            int totalRounds,
            int durationSeconds,
            Func<Guid, int, int, Task> onElapsed
        )
        {
            this.roomId = roomId;
            this.roundNumber = roundNumber;
            this.totalRounds = totalRounds;
            this.durationSeconds = durationSeconds;
            this.onElapsed = onElapsed;

            this.timer = new Timer(OnTimerCallback, null, Timeout.Infinite, Timeout.Infinite);
        }

        public int RemainingSeconds
        {
            get
            {
                var elapsed = (int)(DateTimeOffset.UtcNow - startTime).TotalSeconds;
                return Math.Max(0, durationSeconds - elapsed);
            }
        }

        public void Start() => timer.Change(TimeSpan.FromSeconds(durationSeconds), Timeout.InfiniteTimeSpan);

        private async void OnTimerCallback(object? state)
        {
            try
            {
                await onElapsed(roomId, roundNumber, totalRounds);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void Dispose()
        {
            timer.Dispose();
        }
    }
}