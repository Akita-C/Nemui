using Nemui.Shared.Exceptions.Games.Draw;

public class PlayerNotInRoomException(string message, Guid roomId, string playerId) : DrawGameBaseException(message, "PLAYER_NOT_IN_ROOM", roomId, playerId);