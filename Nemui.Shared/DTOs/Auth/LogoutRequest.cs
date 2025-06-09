namespace Nemui.Shared.DTOs.Auth;

public class LogoutRequest
{
    public string RefreshToken { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
}