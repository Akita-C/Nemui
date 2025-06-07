using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Nemui.Application.Common.Interfaces;

namespace Nemui.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
public class BaseApiController : ControllerBase
{
    private ICurrentUserService? _currentUserService;
    
    protected ICurrentUserService CurrentUserService =>
        _currentUserService ??= HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();

    protected Guid GetCurrentUserId()
    {
        var userIdString = CurrentUserService.UserId;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user token");
        }
        return userId;
    }

    protected string GetCurrentUserEmail() => CurrentUserService.Email ?? string.Empty;
    
    protected string GetCurrentUserName() => CurrentUserService.Name ?? string.Empty;
    
    protected bool IsAuthenticated => CurrentUserService.IsAuthenticated;
}