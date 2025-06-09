using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nemui.Application.Services;
using Nemui.Infrastructure.Configurations;

namespace Nemui.Api.Middlewares;

public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtMiddleware> _logger;
    
    public JwtMiddleware(RequestDelegate next, ILogger<JwtMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IJwtService jwtService)
    {
        var token = ExtractTokenFromHeader(context);

        if (!string.IsNullOrEmpty(token))
        {
            await AttachUserToContextAsync(context, token, jwtService);
        }
        
        await _next(context);
    }

    private string? ExtractTokenFromHeader(HttpContext context)
    {
        var authorizationHeader = context.Request.Headers.Authorization.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(authorizationHeader)) return null;
        
        if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return null;
        
        return authorizationHeader["Bearer ".Length..].Trim();
    }
    
    private async Task AttachUserToContextAsync(HttpContext context, string token, IJwtService jwtService)
    {
        try
        {
            var principal = await jwtService.ValidateTokenAndGetPrincipalAsync(token);
            if (principal != null)
            {
                context.User = principal;
                _logger.LogDebug("JWT token validated successfully for user {UserId}", 
                    principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating JWT token");
        }
    }
}