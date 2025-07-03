using System.Text.Json;
using Nemui.Application.Services.Games.Draw;
using Nemui.Shared.DTOs.Games.Draw;
using Nemui.Shared.Enums;
using Nemui.Shared.Helpers;
using StackExchange.Redis;

namespace Nemui.Infrastructure.Services.Games.Draw;

public class RedisDrawGameService(IDatabase database) : IDrawGameService
{
    private readonly TimeSpan cacheExpirationTime = TimeSpan.FromHours(1);

    // ============================= ROOM METHODS =============================

    public async Task<DrawRoom?> GetRoomAsync(Guid roomId)
    {
        var room = await database.StringGetAsync(GetRoomMetadataKey(roomId));
        return room.HasValue ? JsonSerializer.Deserialize<DrawRoom>(room!) : null;
    }

    public async Task<Guid> CreateRoomAsync(DrawHost host, CreateDrawRoom createRoom)
    {
        var room = new DrawRoom
        {
            RoomId = Guid.NewGuid(),
            RoomName = createRoom.RoomName,
            Host = host,
            Config = createRoom.Config
        };

        await database.StringSetAsync(GetRoomMetadataKey(room.RoomId), JsonSerializer.Serialize(room), cacheExpirationTime);
        var wordCount = (int)(createRoom.Config.MaxRoundPerPlayers * await GetPlayerCountAsync(room.RoomId));
        await InitializeWordPoolAsync(room.RoomId, wordCount);
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
        if (result) await database.KeyExpireAsync(GetRoomPlayerKey(roomId), cacheExpirationTime);
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

    // ============================= WORD POOL METHODS =============================

    public async Task InitializeWordPoolAsync(Guid roomId, int wordCount)
    {
        var key = GetRoomWordPoolKey(roomId);
        var words = WordGenerator.GenerateRandomWords(wordCount);
        await database.SetAddAsync(key, [.. words.Select(w => (RedisValue)w)]);
        await database.KeyExpireAsync(key, cacheExpirationTime);
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
            Phase = Enum.Parse<DrawGamePhase>(gameDict.GetValueOrDefault(GameSessionHaskKey.Phase, "waiting")),
            CurrentRound = int.Parse(gameDict.GetValueOrDefault(GameSessionHaskKey.CurrentRound, "0")),
            TotalRounds = int.Parse(gameDict.GetValueOrDefault(GameSessionHaskKey.TotalRounds, "0")),
            CurrentTurnIndex = int.Parse(gameDict.GetValueOrDefault(GameSessionHaskKey.CurrentTurnIndex, "0")),
            CurrentDrawerId = gameDict.GetValueOrDefault(GameSessionHaskKey.CurrentDrawerId),
            CurrentWord = gameDict.GetValueOrDefault(GameSessionHaskKey.CurrentWord),
            RoundStartTime = DateTime.TryParse(gameDict.GetValueOrDefault(GameSessionHaskKey.RoundStartTime), out var roundStartTime) ? roundStartTime : DateTime.Now,
            SessionStartTime = DateTime.TryParse(gameDict.GetValueOrDefault(GameSessionHaskKey.SessionStartTime), out var sessionStartTime) ? sessionStartTime : DateTime.Now,
            PlayerTurnOrder = [.. turnOrder.Select(order => order.ToString())]
        };
    }

    public async Task<bool> InitializeGameSessionAsync(Guid roomId, List<string> playerIds)
    {
        var room = await GetRoomAsync(roomId);
        if (room == null) return false;

        var transaction = database.CreateTransaction();

        var gameFields = new HashEntry[]
        {
            new (GameSessionHaskKey.Phase, DrawGamePhase.Waiting.ToString()),
            new (GameSessionHaskKey.CurrentRound, "0"),
            new (GameSessionHaskKey.TotalRounds, (room.Config.MaxRoundPerPlayers * playerIds.Count).ToString()),
            new (GameSessionHaskKey.CurrentTurnIndex, "0"),
            new (GameSessionHaskKey.SessionStartTime, DateTime.UtcNow.ToString("O")),
        };

        var task = new List<Task>
        {
            transaction.HashSetAsync(GetRoomGameKey(roomId), gameFields),
            transaction.KeyExpireAsync(GetRoomGameKey(roomId), cacheExpirationTime),
            transaction.ListRightPushAsync(GetRoomTurnOrderKey(roomId), [.. playerIds.Select(id => (RedisValue)id)]),
            transaction.KeyExpireAsync(GetRoomTurnOrderKey(roomId), cacheExpirationTime),
        };

        task.AddRange(playerIds.Select(playerId => transaction.HashSetAsync(GetRoomScoresKey(roomId), playerId, 0)));
        task.Add(transaction.KeyExpireAsync(GetRoomScoresKey(roomId), cacheExpirationTime));

        await Task.WhenAll(task);
        return await transaction.ExecuteAsync();
    }

    public async Task<string?> StartNextRoundAsync(Guid roomId)
    {
        var session = await GetGameSessionAsync(roomId);
        if (session == null) return null;

        var word = await ConsumeRandomWordAsync(roomId);
        if (word == null) return null;

        var newRound = session.CurrentRound + 1;
        var newTurnIndex = newRound % session.PlayerTurnOrder.Count;
        var currentDrawerId = session.PlayerTurnOrder[newTurnIndex];

        var transaction = database.CreateTransaction();
        var updateFields = new HashEntry[]
        {
            new (GameSessionHaskKey.Phase, DrawGamePhase.Drawing.ToString()),
            new (GameSessionHaskKey.CurrentRound, newRound.ToString()),
            new (GameSessionHaskKey.CurrentTurnIndex, newTurnIndex.ToString()),
            new (GameSessionHaskKey.CurrentDrawerId, currentDrawerId),
            new (GameSessionHaskKey.CurrentWord, word),
            new (GameSessionHaskKey.RoundStartTime, DateTime.UtcNow.ToString("O")),
        };

        await transaction.HashSetAsync(GetRoomGameKey(roomId), updateFields);
        await transaction.KeyExpireAsync(GetRoomGameKey(roomId), cacheExpirationTime);

        if (await transaction.ExecuteAsync())
            return word;

        return null;
    }

    public async Task<bool> UpdateGameSessionAsync(Guid roomId, DrawGameSession session)
    {
        var transaction = database.CreateTransaction();

        var updateFields = new HashEntry[]
        {
            new(GameSessionHaskKey.Phase, session.Phase.ToString()),
            new(GameSessionHaskKey.CurrentRound, session.CurrentRound.ToString()),
            new(GameSessionHaskKey.CurrentTurnIndex, session.CurrentTurnIndex.ToString()),
            new(GameSessionHaskKey.CurrentDrawerId, session.CurrentDrawerId ?? ""),
            new(GameSessionHaskKey.CurrentWord, session.CurrentWord ?? ""),
            new(GameSessionHaskKey.RoundStartTime, session.RoundStartTime.ToString("O")),
            new(GameSessionHaskKey.SessionStartTime, session.SessionStartTime.ToString("O"))
        };

        var task = new List<Task>
        {
            transaction.HashSetAsync(GetRoomGameKey(roomId), updateFields),
            transaction.KeyExpireAsync(GetRoomGameKey(roomId), cacheExpirationTime),
        };

        await Task.WhenAll(task);
        return await transaction.ExecuteAsync();
    }

    public async Task<string?> GetCurrentDrawerAsync(Guid roomId)
    {
        var currentDrawer = await database.HashGetAsync(GetRoomGameKey(roomId), GameSessionHaskKey.CurrentDrawerId);
        return currentDrawer.HasValue ? currentDrawer.ToString() : null;
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
        await database.KeyExpireAsync(GetRoomScoresKey(roomId), cacheExpirationTime);
        return result;
    }

    public async Task<long> IncrementPlayerScoreAsync(Guid roomId, string playerId, int increment)
    {
        var newScore = await database.HashIncrementAsync(GetRoomScoresKey(roomId), playerId, increment);
        await database.KeyExpireAsync(GetRoomScoresKey(roomId), cacheExpirationTime);
        return newScore;
    }

    public async Task<bool> ResetPlayerScoresAsync(Guid roomId) => await database.KeyDeleteAsync(GetRoomScoresKey(roomId));

    // ============================= KEY GENERATION METHODS =============================

    public string GetRoomMetadataKey(Guid roomId) => $"room:{roomId}:metadata";
    public string GetRoomPlayerKey(Guid roomId) => $"room:{roomId}:players";
    public string GetRoomGameKey(Guid roomId) => $"room:{roomId}:game";
    public string GetRoomTurnOrderKey(Guid roomId) => $"room:{roomId}:turn_order";
    public string GetRoomScoresKey(Guid roomId) => $"room:{roomId}:scores";
    public string GetRoomWordPoolKey(Guid roomId) => $"room:{roomId}:wordpool";
}

public static class GameSessionHaskKey
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
}