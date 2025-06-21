namespace Nemui.Shared.Exceptions.Games.Draw;

public abstract class DrawGameBaseException : BaseCustomException
{
    public Guid? RoomId { get; set; }
    public string? PlayerId { get; set; }

    protected DrawGameBaseException(string message, string errorCode, Guid? roomId = null, string? playerId = null) 
        : base(message, $"DRAW_GAME_{errorCode}")
    {
        RoomId = roomId;
        PlayerId = playerId;
    }

    protected DrawGameBaseException(string message, Exception innerException, string errorCode, Guid? roomId, string? playerId) 
        : base(message, innerException, $"DRAW_GAME_{errorCode}")
    {
        RoomId = roomId;
        PlayerId = playerId;
    }
}