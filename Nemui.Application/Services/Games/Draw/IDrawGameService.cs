using Nemui.Shared.DTOs.Games.Draw;
using Nemui.Shared.Enums;

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

    // Game session methods
    Task<DrawGameSession?> GetGameSessionAsync(Guid roomId);
    Task<bool> InitializeGameSessionAsync(Guid roomId, List<string> playerIds);
    // Returns the current drawer and the word for the round
    Task<(string?, string?, int)> StartNextRoundAsync(Guid roomId);
    Task<bool> UpdateGamePhaseAsync(Guid roomId, DrawGamePhase phase);
    Task<string?> GetCurrentDrawerAsync(Guid roomId);
    Task<int?> GetCurrentRoundAsync(Guid roomId);
    Task<DrawGamePhase> GetCurrentPhaseAsync(Guid roomId);

    // Word pool methods
    Task InitializeWordPoolAsync(Guid roomId, int wordCount);
    Task<string?> ConsumeRandomWordAsync(Guid roomId);
    Task ResetWordPoolAsync(Guid roomId, int wordCount);

    // Player scores methods
    Task<int> GetPlayerScoreAsync(Guid roomId, string playerId);
    Task<Dictionary<string, int>> GetAllPlayerScoresAsync(Guid roomId);
    Task<bool> SetPlayerScoreAsync(Guid roomId, string playerId, int score);
    Task<long> IncrementPlayerScoreAsync(Guid roomId, string playerId, int increment);
    Task<bool> ResetPlayerScoresAsync(Guid roomId);

    // Key generation methods
    string GetRoomMetadataKey(Guid roomId) => $"room:{roomId}:metadata";
    string GetRoomPlayerKey(Guid roomId) => $"room:{roomId}:players";
    string GetRoomGameKey(Guid roomId) => $"room:{roomId}:game";
    string GetRoomTurnOrderKey(Guid roomId) => $"room:{roomId}:turn_order";
    string GetRoomScoresKey(Guid roomId) => $"room:{roomId}:scores";
    string GetRoomWordPoolKey(Guid roomId) => $"room:{roomId}:wordpool";
}