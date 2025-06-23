using Nemui.Shared.DTOs.Games.Draw;

namespace Nemui.Application.Services.Games.Draw;

public interface IDrawGameService
{
    Task<DrawRoom?> GetRoomAsync(Guid roomId);
    Task<Guid> CreateRoomAsync(DrawHost host, CreateDrawRoom createRoom);
    Task<bool> DeleteRoomAsync(Guid roomId);
    Task<bool> IsRoomExistsAsync(Guid roomId);
    Task<bool> IsRoomFullAsync(Guid roomId);
    Task<bool> AddPlayerAsync(Guid roomId, DrawPlayer player);
    Task<bool> RemovePlayerAsync(Guid roomId, DrawPlayer player);
    Task<long> GetPlayerCountAsync(Guid roomId);
    Task<List<DrawPlayer?>> GetAllPlayersAsync(string playerId, Guid roomId);
    Task<(bool, DrawPlayer?)> IsPlayerInRoomAsync(string playerId, Guid roomId);
    Task<bool> SetRoomStateAsync(Guid roomId, string playerID, string state);
    Task<string?> GetRoomStateAsync(Guid roomId, string playerID);
    string GetRoomKey(Guid roomId) => $"room:{roomId}";
    string GetRoomPlayerKey(Guid roomId) => $"room:{roomId}:player";
    string GetRoomStateKey(Guid roomId) => $"room:{roomId}:state";
}