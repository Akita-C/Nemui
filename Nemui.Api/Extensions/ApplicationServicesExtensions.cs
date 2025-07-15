using FluentValidation;
using Nemui.Api.Extensions.DrawGameExtensions;
using Nemui.Application.Common.Interfaces;
using Nemui.Application.Repositories;
using Nemui.Application.Services;
using Nemui.Application.Validators.Auth;
using Nemui.Infrastructure.Data.Repositories;
using Nemui.Infrastructure.Services;

namespace Nemui.Api.Extensions;

public static class ApplicationServicesExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Repository pattern
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Application services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();

        // Infrastructure services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IJwtBlacklistService, JwtBlacklistService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IImageService, CloudinaryImageService>();

        // Validation
        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

        // HTTP Context
        services.AddHttpContextAccessor();

        return services;
    }
}