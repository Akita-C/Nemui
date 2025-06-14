using FluentValidation;
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
        services.AddScoped<IQuizRepository, QuizRepository>();
        services.AddScoped<IQuestionRepository, QuestionRepository>();
        
        // Application services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IQuizService, QuizService>();
        services.AddScoped<IQuestionService, QuestionService>();
        
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