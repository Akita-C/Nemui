using System.Text.Json;
using Nemui.Application.Services.Games.Draw;
using Nemui.Shared.DTOs.Games.Draw;
using StackExchange.Redis;

namespace Nemui.Infrastructure.Services.Games.Draw;

public class RedisDrawGameService(IDatabase database) : IDrawGameService
{
    private readonly TimeSpan cacheExpirationTime = TimeSpan.FromHours(1);

    public async Task<DrawRoom?> GetRoomAsync(Guid roomId)
    {
        var room = await database.StringGetAsync(GetRoomKey(roomId));
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
        await database.StringSetAsync(GetRoomKey(room.RoomId), JsonSerializer.Serialize(room), cacheExpirationTime);
        return room.RoomId;
    }

    public async Task<bool> DeleteRoomAsync(Guid roomId)
    {
        return await database.KeyDeleteAsync(GetRoomKey(roomId)) && await database.KeyDeleteAsync(GetRoomPlayerKey(roomId));
    }

    public async Task<bool> IsRoomExistsAsync(Guid roomId)
    {
        return await database.KeyExistsAsync(GetRoomKey(roomId));
    }

    public async Task<bool> IsRoomFullAsync(Guid roomId)
    {
        var room = await GetRoomAsync(roomId);
        if (room == null) return false;
        return room.Config.MaxPlayers <= await GetPlayerCountAsync(roomId);
    }

    public async Task<bool> AddPlayerAsync(Guid roomId, DrawPlayer player)
    {
        var isRoomExists = await IsRoomExistsAsync(roomId);
        var (isPlayerAlreadyInRoom, playerInRoom) = await IsPlayerInRoomAsync(player.PlayerId, roomId);
        // Cái này chỉ có thể xảy ra đối với host do khi tạo phòng thì host chưa có connectionId
        if (isPlayerAlreadyInRoom && playerInRoom!.ConnectionId == null) {
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
        if (!result.Any(player => player?.PlayerId == playerId)) throw new PlayerNotInRoomException("Player not in room", roomId, playerId);
        return result;
    }

    public string GetRoomKey(Guid roomId) => $"room:{roomId}";

    public string GetRoomPlayerKey(Guid roomId) => $"room:{roomId}:players";
}