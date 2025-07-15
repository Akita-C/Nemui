using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nemui.Application.Services;
using Nemui.Application.Services.Games.Draw;
using Nemui.Shared.Constants;
using Nemui.Shared.DTOs.Games.Draw;
using Nemui.Shared.Enums;
using Nemui.Shared.Helpers;
using StackExchange.Redis;

namespace Nemui.Infrastructure.Services.Games.Draw;

public class RedisDrawGameService(IDatabase database, IAIService aiService, ILogger<RedisDrawGameService> logger) : IDrawGameService
{
    // ============================= ROOM METHODS =============================

    public async Task<DrawRoom?> GetRoomAsync(Guid roomId)
    {
        var room = await database.StringGetAsync(GetRoomMetadataKey(roomId));
        return room.HasValue ? JsonSerializer.Deserialize<DrawRoom>(room!) : null;
    }

    public async Task<DrawRoomConfig?> GetRoomConfigAsync(Guid roomId)
    {
        var room = await GetRoomAsync(roomId);
        return room?.Config;
    }

    public async Task<Guid> CreateRoomAsync(DrawHost host, CreateDrawRoom createRoom)
    {
        logger.LogInformation("Creating room {RoomName} with theme {Theme}", createRoom.RoomName, createRoom.Theme);

        var room = new DrawRoom
        {
            RoomId = Guid.NewGuid(),
            RoomName = createRoom.RoomName,
            Theme = createRoom.Theme,
            Host = host,
            Config = createRoom.Config
        };

        await database.StringSetAsync(GetRoomMetadataKey(room.RoomId), JsonSerializer.Serialize(room), CalculateGameExpirationTime(createRoom.Config));
        await database.HashSetAsync(GetRoomGameKey(room.RoomId),
        [
            new (GameSessionHashKey.Phase, DrawGamePhase.Waiting.ToString()),
            new (GameSessionHashKey.GameExpirationTime, CalculateGameExpirationTime(createRoom.Config).TotalSeconds.ToString()),
        ]);
        await database.KeyExpireAsync(GetRoomGameKey(room.RoomId), CalculateGameExpirationTime(createRoom.Config));
        return room.RoomId;
    }

    public async Task<bool> DeleteRoomAsync(Guid roomId)
    {
        var keys = new RedisKey[]
        {
            GetRoomMetadataKey(roomId),
            GetRoomPlayerKey(roomId),
            GetRoomGameKey(roomId),
            GetRoomTurnOrderKey(roomId),
            GetRoomScoresKey(roomId),
            GetRoomWordPoolKey(roomId)
        };
        return await database.KeyDeleteAsync(keys) > 0;
    }

    public async Task<bool> IsRoomExistsAsync(Guid roomId)
    {
        return await database.KeyExistsAsync(GetRoomMetadataKey(roomId));
    }

    public async Task<bool> IsRoomFullAsync(Guid roomId)
    {
        var room = await GetRoomAsync(roomId);
        if (room == null) return false;
        return room.Config.MaxPlayers <= await GetPlayerCountAsync(roomId);
    }

    // ============================= PLAYER METHODS =============================

    public async Task<bool> AddPlayerAsync(Guid roomId, DrawPlayer player)
    {
        var isRoomExists = await IsRoomExistsAsync(roomId);
        var (isPlayerAlreadyInRoom, playerInRoom) = await IsPlayerInRoomAsync(player.PlayerId, roomId);

        // Handle host reconnection case
        if (isPlayerAlreadyInRoom && playerInRoom!.ConnectionId == null)
        {
            await database.SetRemoveAsync(GetRoomPlayerKey(roomId), JsonSerializer.Serialize(playerInRoom));
        }

        var result = isRoomExists && await database.SetAddAsync(GetRoomPlayerKey(roomId), JsonSerializer.Serialize(player));
        if (result) await database.KeyExpireAsync(GetRoomPlayerKey(roomId), await GetGameExpirationTimeAsync(roomId));
        return result;
    }

    public async Task<bool> RemovePlayerAsync(Guid roomId, DrawPlayer player)
    {
        var isRoomExists = await IsRoomExistsAsync(roomId);
        return isRoomExists && await database.SetRemoveAsync(GetRoomPlayerKey(roomId), JsonSerializer.Serialize(player));
    }

    public async Task<long> GetPlayerCountAsync(Guid roomId)
    {
        return await database.SetLengthAsync(GetRoomPlayerKey(roomId));
    }

    public async Task<(bool, DrawPlayer?)> IsPlayerInRoomAsync(string playerId, Guid roomId)
    {
        var players = await database.SetMembersAsync(GetRoomPlayerKey(roomId));
        var player = players.FirstOrDefault(player => player.HasValue && JsonSerializer.Deserialize<DrawPlayer>(player!)!.PlayerId == playerId);
        return (player.HasValue, player.HasValue ? JsonSerializer.Deserialize<DrawPlayer>(player!) : null);
    }

    public async Task<List<DrawPlayer?>> GetAllPlayersAsync(string playerId, Guid roomId)
    {
        var players = await database.SetMembersAsync(GetRoomPlayerKey(roomId));
        List<DrawPlayer?> result = [.. players.Where(player => player.HasValue).Select(player => JsonSerializer.Deserialize<DrawPlayer>(player!))];
        if (!result.Any(player => player?.PlayerId == playerId))
            throw new PlayerNotInRoomException("Player not in room", roomId, playerId);
        return result;
    }

    public async Task<DrawPlayer?> GetPlayerAsync(string playerId, Guid roomId)
    {
        var players = await database.SetMembersAsync(GetRoomPlayerKey(roomId));
        var player = players.FirstOrDefault(player => player.HasValue && JsonSerializer.Deserialize<DrawPlayer>(player!)!.PlayerId == playerId);
        return player.HasValue ? JsonSerializer.Deserialize<DrawPlayer>(player!) : null;
    }

    // ============================= WORD POOL METHODS =============================

    public async Task InitializeWordPoolAsync(Guid roomId, int wordCount)
    {
        var room = await GetRoomAsync(roomId);
        var key = GetRoomWordPoolKey(roomId);
        var words = (await aiService.GenerateWords<GenerateWordsResponse>(room?.Theme ?? throw new InvalidOperationException("Room theme is not set"), wordCount)).Words;
        await database.SetAddAsync(key, [.. words.Select(w => (RedisValue)w)]);
        await database.KeyExpireAsync(key, await GetGameExpirationTimeAsync(roomId));
    }

    public async Task<string?> ConsumeRandomWordAsync(Guid roomId)
    {
        var key = GetRoomWordPoolKey(roomId);
        var word = await database.SetPopAsync(key);
        return word.HasValue ? word.ToString() : null;
    }

    public async Task ResetWordPoolAsync(Guid roomId, int wordCount)
    {
        var key = GetRoomWordPoolKey(roomId);
        await database.KeyDeleteAsync(key);
        await InitializeWordPoolAsync(roomId, wordCount);
    }

    // ============================= GAME SESSION METHODS =============================

    public async Task<DrawGameSession?> GetGameSessionAsync(Guid roomId)
    {
        var gameData = await database.HashGetAllAsync(GetRoomGameKey(roomId));
        if (!gameData.Any()) return null;

        var gameDict = gameData.ToDictionary(kvp => kvp.Name.ToString(), kvp => kvp.Value.ToString());
        var turnOrder = await database.ListRangeAsync(GetRoomTurnOrderKey(roomId));

        return new DrawGameSession
        {
            RoomId = roomId,
            Phase = Enum.Parse<DrawGamePhase>(gameDict.GetValueOrDefault(GameSessionHashKey.Phase, "waiting")),
            CurrentRound = int.Parse(gameDict.GetValueOrDefault(GameSessionHashKey.CurrentRound, "0")),
            TotalRounds = int.Parse(gameDict.GetValueOrDefault(GameSessionHashKey.TotalRounds, "0")),
            CurrentTurnIndex = int.Parse(gameDict.GetValueOrDefault(GameSessionHashKey.CurrentTurnIndex, "0")),
            CurrentDrawerId = gameDict.GetValueOrDefault(GameSessionHashKey.CurrentDrawerId),
            CurrentWord = gameDict.GetValueOrDefault(GameSessionHashKey.CurrentWord),
            RoundStartTime = DateTime.TryParse(gameDict.GetValueOrDefault(GameSessionHashKey.RoundStartTime), out var roundStartTime) ? roundStartTime : DateTime.Now,
            SessionStartTime = DateTime.TryParse(gameDict.GetValueOrDefault(GameSessionHashKey.SessionStartTime), out var sessionStartTime) ? sessionStartTime : DateTime.Now,
            PlayerTurnOrder = [.. turnOrder.Select(order => order.ToString())]
        };
    }

    public async Task<bool> InitializeGameSessionAsync(Guid roomId, List<string> playerIds, int totalRounds)
    {
        var room = await GetRoomAsync(roomId);
        if (room == null) return false;

        var transaction = database.CreateTransaction();

        var gameFields = new HashEntry[]
        {
            new (GameSessionHashKey.Phase, DrawGamePhase.Waiting.ToString()),
            new (GameSessionHashKey.CurrentRound, "0"),
            new (GameSessionHashKey.TotalRounds, totalRounds.ToString()),
            new (GameSessionHashKey.CurrentTurnIndex, "0"),
            new (GameSessionHashKey.SessionStartTime, DateTime.UtcNow.ToString("O")),
        };

        var task = new List<Task>
        {
            transaction.HashSetAsync(GetRoomGameKey(roomId), gameFields),
            transaction.KeyExpireAsync(GetRoomGameKey(roomId), await GetGameExpirationTimeAsync(roomId)),
            transaction.ListRightPushAsync(GetRoomTurnOrderKey(roomId), [.. playerIds.Select(id => (RedisValue)id)]),
            transaction.KeyExpireAsync(GetRoomTurnOrderKey(roomId), await GetGameExpirationTimeAsync(roomId)),
        };

        task.AddRange(playerIds.Select(playerId => transaction.HashSetAsync(GetRoomScoresKey(roomId), playerId, 0)));
        task.Add(transaction.KeyExpireAsync(GetRoomScoresKey(roomId), await GetGameExpirationTimeAsync(roomId)));

        var result = await transaction.ExecuteAsync();
        if (!result) return false;
        await Task.WhenAll(task);
        return true;
    }

    public async Task<(string?, string?, int)> StartNextRoundAsync(Guid roomId)
    {
        var session = await GetGameSessionAsync(roomId);
        if (session == null) return (null, null, 0);

        var word = await ConsumeRandomWordAsync(roomId);
        if (word == null) return (null, null, 0);

        var newRound = session.CurrentRound + 1;
        var newTurnIndex = newRound % session.PlayerTurnOrder.Count;
        var currentDrawerId = session.PlayerTurnOrder[newTurnIndex];

        var transaction = database.CreateTransaction();
        var updateFields = new HashEntry[]
        {
            new (GameSessionHashKey.Phase, DrawGamePhase.Drawing.ToString()),
            new (GameSessionHashKey.CurrentRound, newRound.ToString()),
            new (GameSessionHashKey.CurrentTurnIndex, newTurnIndex.ToString()),
            new (GameSessionHashKey.CurrentDrawerId, currentDrawerId),
            new (GameSessionHashKey.CurrentWord, word),
            new (GameSessionHashKey.RoundStartTime, DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
        };

        var tasks = new List<Task>
        {
            transaction.HashSetAsync(GetRoomGameKey(roomId), updateFields),
            transaction.KeyExpireAsync(GetRoomGameKey(roomId), await GetGameExpirationTimeAsync(roomId)),
        };

        var result = await transaction.ExecuteAsync();
        if (!result) return (null, null, 0);
        await Task.WhenAll(tasks);
        return (currentDrawerId, word, newRound);
    }

    public async Task<bool> UpdateGamePhaseAsync(Guid roomId, DrawGamePhase phase)
    {
        try
        {
            var updateFields = new HashEntry[]
            {
                new(GameSessionHashKey.Phase, phase.ToString()),
            };
            await database.HashSetAsync(GetRoomGameKey(roomId), updateFields);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating game phase for room {RoomId}", roomId);
            return false;
        }
    }

    public async Task<string?> GetCurrentDrawerAsync(Guid roomId)
    {
        var currentDrawer = await database.HashGetAsync(GetRoomGameKey(roomId), GameSessionHashKey.CurrentDrawerId);
        return currentDrawer.HasValue ? currentDrawer.ToString() : null;
    }

    public async Task<string?> GetCurrentWordAsync(Guid roomId)
    {
        var currentWord = await database.HashGetAsync(GetRoomGameKey(roomId), GameSessionHashKey.CurrentWord);
        return currentWord.HasValue ? currentWord.ToString() : null;
    }

    public async Task<int?> GetCurrentRoundAsync(Guid roomId)
    {
        var currentRound = await database.HashGetAsync(GetRoomGameKey(roomId), GameSessionHashKey.CurrentRound);
        return currentRound.HasValue ? (int)currentRound : null;
    }

    public async Task<DrawGamePhase> GetCurrentPhaseAsync(Guid roomId)
    {
        var phase = await database.HashGetAsync(GetRoomGameKey(roomId), GameSessionHashKey.Phase);
        return Enum.Parse<DrawGamePhase>(phase.HasValue ? phase.ToString() : "waiting");
    }

    public async Task<TimeSpan> GetGameExpirationTimeAsync(Guid roomId)
    {
        var gameExpirationTime = await database.HashGetAsync(GetRoomGameKey(roomId), GameSessionHashKey.GameExpirationTime);
        return gameExpirationTime.HasValue ? TimeSpan.FromSeconds((long)gameExpirationTime) : TimeSpan.Zero;
    }

    // ============================= PLAYER SCORES METHODS =============================
    public async Task<int> GetPlayerScoreAsync(Guid roomId, string playerId)
    {
        var score = await database.HashGetAsync(GetRoomScoresKey(roomId), playerId);
        return score.HasValue ? (int)score : 0;
    }

    public async Task<Dictionary<string, int>> GetAllPlayerScoresAsync(Guid roomId)
    {
        var scores = await database.HashGetAllAsync(GetRoomScoresKey(roomId));
        return scores.ToDictionary(kvp => kvp.Name.ToString(), kvp => (int)kvp.Value);
    }

    public async Task<bool> SetPlayerScoreAsync(Guid roomId, string playerId, int score)
    {
        var result = await database.HashSetAsync(GetRoomScoresKey(roomId), playerId, score);
        await database.KeyExpireAsync(GetRoomScoresKey(roomId), await GetGameExpirationTimeAsync(roomId));
        return result;
    }

    public async Task<long> IncrementPlayerScoreAsync(Guid roomId, string playerId, int increment)
    {
        var newScore = await database.HashIncrementAsync(GetRoomScoresKey(roomId), playerId, increment);
        await database.KeyExpireAsync(GetRoomScoresKey(roomId), await GetGameExpirationTimeAsync(roomId));
        return newScore;
    }

    public async Task<bool> ResetPlayerScoresAsync(Guid roomId) => await database.KeyDeleteAsync(GetRoomScoresKey(roomId));

    // ============================= PLAYER HEARTS METHODS =============================
    public async Task InitializePlayerHeartsAsync(Guid roomId, List<string> playerIds)
    {
        var entries = playerIds.Select(playerId => new HashEntry(playerId, DrawConstants.MaxHearts)).ToArray();
        await database.HashSetAsync(GetRoomPlayerHeartsKey(roomId), entries);
        await database.KeyExpireAsync(GetRoomPlayerHeartsKey(roomId), await GetGameExpirationTimeAsync(roomId));
    }

    public async Task<int> GetPlayerHeartsAsync(Guid roomId, string playerId)
    {
        var hearts = await database.HashGetAsync(GetRoomPlayerHeartsKey(roomId), playerId);
        return hearts.HasValue ? (int)hearts : 0;
    }

    public async Task<bool> DecrementPlayerHeartsAsync(Guid roomId, string playerId)
    {
        var result = await database.HashDecrementAsync(GetRoomPlayerHeartsKey(roomId), playerId, 1);
        return result > 0;
    }

    public async Task<bool> SetPlayerHeartsAsync(Guid roomId, string playerId, int hearts)
    {
        var result = await database.HashSetAsync(GetRoomPlayerHeartsKey(roomId), playerId, hearts);
        await database.KeyExpireAsync(GetRoomPlayerHeartsKey(roomId), await GetGameExpirationTimeAsync(roomId));
        return result;
    }

    public async Task<bool> ResetAllPlayerHeartsAsync(Guid roomId)
    {
        try
        {
            var players = await database.HashGetAllAsync(GetRoomPlayerHeartsKey(roomId));
            var entries = players.Select(player => new HashEntry(player.Name, DrawConstants.MaxHearts)).ToArray();
            await database.HashSetAsync(GetRoomPlayerHeartsKey(roomId), entries);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resetting all player hearts for room {RoomId}DrawGame", roomId);
            return false;
        }
    }

    // ============================= PLAYER GUESS METHODS =============================
    public async Task<bool> CheckAllPlayersGuessedAsync(Guid roomId)
    {
        var playerHearts = await database.HashGetAllAsync(GetRoomPlayerHeartsKey(roomId));
        if (playerHearts.Length == 0) return false;
        var currentDrawerId = await GetCurrentDrawerAsync(roomId);
        if (currentDrawerId == null) return false;

        return playerHearts
            .Where(playerHeart => playerHeart.Name != currentDrawerId)
            .All(playerHealth => (int)playerHealth.Value <= 0);
    }


    public async Task<(bool isCorrect, int score, bool isAllPlayersGuessed)> GuessWordAsync(Guid roomId, string playerId, string message)
    {
        var currentWord = await GetCurrentWordAsync(roomId);
        if (currentWord == null) return (isCorrect: false, score: 0, isAllPlayersGuessed: false);

        var isCorrect = currentWord.Equals(message, StringComparison.OrdinalIgnoreCase);
        if (!isCorrect) return (isCorrect: false, score: 0, isAllPlayersGuessed: false);
        var scoreToAdd = await CalculateScoreBasedOnTimeAsync(roomId);
        var newScore = await IncrementPlayerScoreAsync(roomId, playerId, scoreToAdd);
        await SetPlayerHeartsAsync(roomId, playerId, 0);
        var isAllPlayersGuessed = await CheckAllPlayersGuessedAsync(roomId);
        return (isCorrect: true, score: (int)newScore, isAllPlayersGuessed);
    }

    // ============================= ROUND TIMING METHODS =============================

    public async Task SetRoundStartTimeAsync(Guid roomId, DateTimeOffset startTime)
    {
        var updateFields = new HashEntry[]
        {
            new (GameSessionHashKey.RoundStartTime, startTime.ToUnixTimeSeconds()),
        };
        await database.HashSetAsync(GetRoomGameKey(roomId), updateFields);
    }

    public async Task<DateTimeOffset?> GetRoundStartTimeAsync(Guid roomId)
    {
        var roundStartTime = await database.HashGetAsync(GetRoomGameKey(roomId), GameSessionHashKey.RoundStartTime);
        return roundStartTime.HasValue ? DateTimeOffset.FromUnixTimeSeconds((long)roundStartTime) : null;
    }

    // ============================= KEY GENERATION METHODS =============================

    public string GetRoomMetadataKey(Guid roomId) => $"room:{roomId}:metadata";
    public string GetRoomPlayerKey(Guid roomId) => $"room:{roomId}:players";
    public string GetRoomGameKey(Guid roomId) => $"room:{roomId}:game";
    public string GetRoomTurnOrderKey(Guid roomId) => $"room:{roomId}:turn_order";
    public string GetRoomScoresKey(Guid roomId) => $"room:{roomId}:scores";
    public string GetRoomPlayerHeartsKey(Guid roomId) => $"room:{roomId}:hearts";
    public string GetRoomWordPoolKey(Guid roomId) => $"room:{roomId}:wordpool";

    private async Task<int> CalculateScoreBasedOnTimeAsync(Guid roomId)
    {
        try
        {
            var roundStartTime = await GetRoundStartTimeAsync(roomId);
            var roomConfig = await GetRoomConfigAsync(roomId);

            if (roundStartTime == null || roomConfig == null) return 1;

            var elapsedTime = (DateTimeOffset.UtcNow - roundStartTime.Value).TotalSeconds;
            var totalTime = roomConfig.DrawingDurationSeconds + roomConfig.GuessingDurationSeconds;

            var remainingTime = totalTime - elapsedTime;
            var score = (int)Math.Round(100.0 * (remainingTime / totalTime));

            return Math.Max(1, score);
        }
        catch (Exception)
        {
            // Fallback to original score on any error
            return 1;
        }
    }

    // 100% Need to improve this, but it's a good start
    private static TimeSpan CalculateGameExpirationTime(DrawRoomConfig config)
    {
        var totalRounds = config.MaxRoundPerPlayers * config.MaxPlayers;
        var totalTime = totalRounds * (config.DrawingDurationSeconds + config.GuessingDurationSeconds + config.RevealDurationSeconds);
        return TimeSpan.FromSeconds(totalTime) + TimeSpan.FromMinutes(5); // 5 minutes for players in room do other stuff
    }
}

public static class GameSessionHashKey
{
    public const string Phase = "phase";
    public const string CurrentRound = "current_round";
    public const string TotalRounds = "total_rounds";
    public const string CurrentDrawerId = "current_drawer_id";
    public const string CurrentWord = "current_word";
    public const string PlayerTurnOrder = "player_turn_order";
    public const string CurrentTurnIndex = "current_turn_index";
    public const string SessionStartTime = "session_start_time";
    public const string RoundStartTime = "round_start_time";
    public const string GameExpirationTime = "game_expiration_time";
}