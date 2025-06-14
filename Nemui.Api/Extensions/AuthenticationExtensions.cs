using Microsoft.AspNetCore.Authentication.JwtBearer;
using Nemui.Infrastructure.Configurations;

namespace Nemui.Api.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddCustomAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;
        
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = jwtSettings.CreateTokenValidationParameters();
        });
        
        services.AddAuthorization();
        
        return services;
    }
} 