using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Nemui.Application.Services;
using Nemui.Infrastructure.Configurations;
using Nemui.Infrastructure.Services.AIs;

namespace Nemui.Api.Extensions;

public static class AIServicesExtensions
{
    public static IServiceCollection AddAIServices(this IServiceCollection services)
    {
        var aiSettings = services.BuildServiceProvider().GetRequiredService<IOptions<AISettings>>();

        if (aiSettings.Value.ApiKey is null || aiSettings.Value.Model is null)
            throw new InvalidOperationException("AI settings are not configured");

        services.AddKernel()
            .AddGoogleAIGeminiChatCompletion(
                modelId: aiSettings.Value.Model,
                apiKey: aiSettings.Value.ApiKey
            );

        services.AddSingleton<IAIService, GeminiService>();

        return services;
    }
}