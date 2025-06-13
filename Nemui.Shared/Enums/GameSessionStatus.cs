namespace Nemui.Shared.Enums;

public enum GameSessionStatus
{
    Waiting = 1,        // Đang chờ người chơi
    InProgress = 2,     // Đang chơi
    Paused = 3,         // Tạm dừng
    Completed = 4,      // Hoàn thành
    Cancelled = 5       // Đã hủy
}