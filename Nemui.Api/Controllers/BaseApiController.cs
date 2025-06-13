using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Nemui.Application.Common.Interfaces;
using Nemui.Shared.DTOs.Common;

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
    
    protected static PagedResponse<T> CreatePagedResponse<T>(List<T> data, string? nextCursor) => new()
    {
        Data = data,
        NextCursor = nextCursor,
        HasNextPage = !string.IsNullOrEmpty(nextCursor)
    };
    
    protected async Task<IActionResult> ExecuteWithErrorHandlingAsync(
        Func<Task<IActionResult>> operation, 
        string operationDescription,
        ILogger logger)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error {Operation}", operationDescription);
            return StatusCode(500, ErrorResponse.Create($"An error occurred while {operationDescription}"));
        }
    }
    
    protected async Task<IActionResult> ExecuteWithAuthenticationAsync(
        Func<Guid, Task<IActionResult>> operation, 
        string operationDescription,
        ILogger logger)
    {
        try
        {
            var userId = GetCurrentUserId();
            return await operation(userId);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("{Operation} failed: {Message}", operationDescription, ex.Message);
            return ex.Message.Contains("not found") 
                ? NotFound(ErrorResponse.Create(ex.Message))
                : Conflict(ErrorResponse.Create(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning("Unauthorized {Operation} attempt: {Message}", operationDescription, ex.Message);
            return Unauthorized(ErrorResponse.Create(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error {Operation}", operationDescription);
            return StatusCode(500, ErrorResponse.Create($"An error occurred while {operationDescription}"));
        }
    }
}