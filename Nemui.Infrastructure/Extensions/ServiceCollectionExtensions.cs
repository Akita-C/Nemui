using Microsoft.Extensions.DependencyInjection;
using Nemui.Infrastructure.Data.Seeds;

namespace Nemui.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSeeders(this IServiceCollection services)
    {
        services.AddScoped<SeederManager>();
        services.AddScoped<ISeeder, QuizSeeder>();

        return services;
    }
}