using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nemui.Application.Services;
using Nemui.Shared.DTOs.Auth;
using Nemui.Shared.DTOs.Common;

namespace Nemui.Api.Controllers;

[Authorize]
public class UsersController : BaseApiController
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;
    
    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }
    
    [HttpPost("avatar")]
    [ProducesResponseType(typeof(ApiResponse<ImageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateAvatarAsync([FromForm] UpdateAvatarRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _userService.UpdateUserAvatarAsync(userId, request, cancellationToken);
            
            _logger.LogInformation("Avatar updated successfully for user {UserId}", userId);
            
            return Ok(ApiResponse<ImageResponse>.SuccessResult(result, "Avatar updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Avatar update failed: {Message}", ex.Message);
            return BadRequest(ErrorResponse.Create(ex.Message));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid avatar file: {Message}", ex.Message);
            return BadRequest(ErrorResponse.Create(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized avatar update attempt: {Message}", ex.Message);
            return Unauthorized(ErrorResponse.Create(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating avatar for user");
            return StatusCode(500, ErrorResponse.Create("An error occurred while updating avatar"));
        }
    }
    
    [HttpDelete("avatar")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteAvatarAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _userService.DeleteUserAvatarAsync(userId, cancellationToken);
            
            _logger.LogInformation("Avatar deleted for user {UserId}", userId);
            
            return Ok(ApiResponse<bool>.SuccessResult(result, "Avatar deleted successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized avatar deletion attempt: {Message}", ex.Message);
            return Unauthorized(ErrorResponse.Create(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting avatar for user");
            return StatusCode(500, ErrorResponse.Create("An error occurred while deleting avatar"));
        }
    }

    [HttpPut("profile")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateProfileAsync([FromBody] UpdateUserProfileRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _userService.UpdateUserProfileAsync(userId, request, cancellationToken);
            
            _logger.LogInformation("Profile updated successfully for user {UserId}", userId);
            
            return Ok(ApiResponse<bool>.SuccessResult(result, "Profile updated successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized profile update attempt: {Message}", ex.Message);
            return Unauthorized(ErrorResponse.Create(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user");
            return StatusCode(500, ErrorResponse.Create("An error occurred while updating profile"));
        }
    }

    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _userService.ChangePasswordAsync(userId, request, cancellationToken);
            
            _logger.LogInformation("Password changed successfully for user {UserId}", userId);
            
            return Ok(ApiResponse<bool>.SuccessResult(result, "Password changed successfully. Please login again."));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Password change failed for user: {Message}", ex.Message);
            return Unauthorized(ErrorResponse.Create(ex.Message));
        }
        catch (System.ComponentModel.DataAnnotations.ValidationException ex)
        {
            _logger.LogWarning("Password validation failed: {Message}", ex.Message);
            return BadRequest(ErrorResponse.Create(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user");
            return StatusCode(500, ErrorResponse.Create("An error occurred while changing password"));
        }
    }
}