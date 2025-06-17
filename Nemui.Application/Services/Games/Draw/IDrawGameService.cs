using Nemui.Shared.DTOs.Games.Draw;

namespace Nemui.Application.Services.Games.Draw;

public interface IDrawGameService
{
    Task<DrawRoom?> GetRoomAsync(Guid roomId, CancellationToken cancellationToken = default);
    Task<Guid> CreateRoomAsync(DrawHost host, CreateDrawRoom createRoom, CancellationToken cancellationToken = default);
    string GetRoomName(Guid roomId) => $"room:{roomId}";
}