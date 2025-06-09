using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Nemui.Application.Services;
using Nemui.Shared.Constants;
using Nemui.Shared.DTOs.Auth;
using Nemui.Shared.DTOs.Common;

namespace Nemui.Api.Controllers;

public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;
    
    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }
    
    [HttpPost("login")]
    [EnableRateLimiting("AuthPolicy")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _authService.LoginAsync(request, cancellationToken);
            
            _logger.LogInformation("User {Email} logged in successfully", request.Email);
            
            return Ok(ApiResponse<AuthResponse>.SuccessResult(result, "Login successful"));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Failed login attempt for {Email}: {Message}", request.Email, ex.Message);
            return Unauthorized(ErrorResponse.Create(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", request.Email);
            return StatusCode(500, ErrorResponse.Create("An error occurred during login"));
        }
    }
    
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _authService.RegisterAsync(request, cancellationToken);
            
            _logger.LogInformation("User {Email} registered successfully", request.Email);
            
            return CreatedAtAction(nameof(GetProfileAsync), new { }, 
                ApiResponse<AuthResponse>.SuccessResult(result, "Registration successful"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Registration failed for {Email}: {Message}", request.Email, ex.Message);
            return Conflict(ErrorResponse.Create(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for {Email}", request.Email);
            return StatusCode(500, ErrorResponse.Create("An error occurred during registration"));
        }
    }
    
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RefreshTokenAsync([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);
            
            _logger.LogInformation("Token refreshed successfully");
            
            return Ok(ApiResponse<AuthResponse>.SuccessResult(result, "Token refreshed successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Token refresh failed: {Message}", ex.Message);
            return Unauthorized(ErrorResponse.Create(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, ErrorResponse.Create("An error occurred during token refresh"));
        }
    }
    
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> LogoutAsync([FromBody] LogoutRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(request.AccessToken)) await _authService.BlacklistAccessTokenAsync(request.AccessToken, cancellationToken);
            
            var result = await _authService.LogoutAsync(request.RefreshToken, cancellationToken);
            
            _logger.LogInformation("User logged out successfully");
            
            return Ok(ApiResponse<bool>.SuccessResult(result, "Logout successful"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, ErrorResponse.Create("An error occurred during logout"));
        }
    }
    
    [HttpPost("revoke")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RevokeTokenAsync([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _authService.RevokeTokenAsync(request.RefreshToken, cancellationToken);
            
            _logger.LogInformation("Token revoked successfully");
            
            return Ok(ApiResponse<bool>.SuccessResult(result, "Token revoked successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token revocation");
            return StatusCode(500, ErrorResponse.Create("An error occurred during token revocation"));
        }
    }
    
    [HttpPost("revoke-all")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RevokeAllTokensAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _authService.RevokeAllUserTokensAsync(userId, cancellationToken);
            
            _logger.LogInformation("All tokens revoked for user {UserId}", userId);
            
            return Ok(ApiResponse<bool>.SuccessResult(result, "All tokens revoked successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Invalid user token during revoke all: {Message}", ex.Message);
            return Unauthorized(ErrorResponse.Create(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during revoke all tokens");
            return StatusCode(500, ErrorResponse.Create("An error occurred during token revocation"));
        }
    }
    
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var email = GetCurrentUserEmail();
            var name = GetCurrentUserName();
            
            var profile = new UserProfileDto
            {
                Id = userId,
                Email = email,
                Name = name,
                Role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty,
                IsEmailVerified = bool.TryParse(User.FindFirst(AuthConstants.ClaimTypes.EmailVerified)?.Value, out var emailVerified) && emailVerified
            };

            _logger.LogInformation("Profile retrieved for user {UserId}", userId);

            return Task.FromResult<IActionResult>(Ok(ApiResponse<UserProfileDto>.SuccessResult(profile, "Profile retrieved successfully")));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Invalid user token during profile retrieval: {Message}", ex.Message);
            return Task.FromResult<IActionResult>(Unauthorized(ErrorResponse.Create(ex.Message)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile");
            return Task.FromResult<IActionResult>(StatusCode(500, ErrorResponse.Create("An error occurred while retrieving profile")));
        }
    }
    
    // [HttpPost("change-password")]
    // [Authorize]
    // [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    // [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    // [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    // [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    // public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken = default)
    // {
    //     try
    //     {
    //         var userId = GetCurrentUserId();
    //         var result = await _authService.ChangePasswordAsync(userId, request, cancellationToken);
    //         
    //         _logger.LogInformation("Password changed successfully for user {UserId}", userId);
    //         
    //         return Ok(ApiResponse<bool>.SuccessResult(result, "Password changed successfully"));
    //     }
    //     catch (UnauthorizedAccessException ex)
    //     {
    //         _logger.LogWarning("Password change failed for invalid token: {Message}", ex.Message);
    //         return Unauthorized(ErrorResponse.Create(ex.Message));
    //     }
    //     catch (InvalidOperationException ex)
    //     {
    //         _logger.LogWarning("Password change failed: {Message}", ex.Message);
    //         return BadRequest(ErrorResponse.Create(ex.Message));
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error during password change");
    //         return StatusCode(500, ErrorResponse.Create("An error occurred during password change"));
    //     }
    // }
    //
    // [HttpPost("verify-email")]
    // [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    // [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    // [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    // public async Task<IActionResult> VerifyEmailAsync([FromBody] VerifyEmailRequest request, CancellationToken cancellationToken = default)
    // {
    //     try
    //     {
    //         var result = await _authService.VerifyEmailAsync(request.Token, cancellationToken);
    //         
    //         _logger.LogInformation("Email verified successfully");
    //         
    //         return Ok(ApiResponse<bool>.SuccessResult(result, "Email verified successfully"));
    //     }
    //     catch (InvalidOperationException ex)
    //     {
    //         _logger.LogWarning("Email verification failed: {Message}", ex.Message);
    //         return BadRequest(ErrorResponse.Create(ex.Message));
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error during email verification");
    //         return StatusCode(500, ErrorResponse.Create("An error occurred during email verification"));
    //     }
    // }
    //
    // [HttpPost("forgot-password")]
    // [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    // [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    // [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    // public async Task<IActionResult> ForgotPasswordAsync([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    // {
    //     try
    //     {
    //         var result = await _authService.ForgotPasswordAsync(request.Email, cancellationToken);
    //         
    //         _logger.LogInformation("Password reset requested for email {Email}", request.Email);
    //         
    //         return Ok(ApiResponse<bool>.SuccessResult(result, "Password reset email sent"));
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error during password reset request");
    //         return StatusCode(500, ErrorResponse.Create("An error occurred during password reset request"));
    //     }
    // }
    //
    // [HttpPost("reset-password")]
    // [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    // [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    // [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    // public async Task<IActionResult> ResetPasswordAsync([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken = default)
    // {
    //     try
    //     {
    //         var result = await _authService.ResetPasswordAsync(request, cancellationToken);
    //         
    //         _logger.LogInformation("Password reset successfully");
    //         
    //         return Ok(ApiResponse<bool>.SuccessResult(result, "Password reset successfully"));
    //     }
    //     catch (InvalidOperationException ex)
    //     {
    //         _logger.LogWarning("Password reset failed: {Message}", ex.Message);
    //         return BadRequest(ErrorResponse.Create(ex.Message));
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error during password reset");
    //         return StatusCode(500, ErrorResponse.Create("An error occurred during password reset"));
    //     }
    // }
}