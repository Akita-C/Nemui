using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Nemui.Application.Services.Games.Draw;
using Nemui.Shared.DTOs.Games.Draw;

namespace Nemui.Infrastructure.Services.Games.Draw;

public class RedisDrawGameService(IDistributedCache cache) : IDrawGameService
{
    private readonly DistributedCacheEntryOptions cacheEntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
    };
    
    public async Task<DrawRoom?> GetRoomAsync(Guid roomId, CancellationToken cancellationToken = default)
    {
        var room = await cache.GetStringAsync(GetRoomName(roomId), cancellationToken);
        return room is null ? null : JsonSerializer.Deserialize<DrawRoom>(room);
    } 

    public async Task<Guid> CreateRoomAsync(DrawHost host, CreateDrawRoom createRoom, CancellationToken cancellationToken = default)
    {
        var room = new DrawRoom
        {
            RoomId = Guid.NewGuid(),
            RoomName = createRoom.RoomName,
            DrawHost = host,
            Config = new DrawRoomConfig
            {
                MaxPlayers = createRoom.Config.MaxPlayers,
                MaxRoundPerPlayers = createRoom.Config.MaxRoundPerPlayers,
            }
        };
        await cache.SetStringAsync(GetRoomName(room.RoomId), JsonSerializer.Serialize(room), cacheEntryOptions, cancellationToken);
        return room.RoomId;
    }

    public string GetRoomName(Guid roomId) => $"room:{roomId}";
}