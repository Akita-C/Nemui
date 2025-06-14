using Microsoft.EntityFrameworkCore;
using Nemui.Infrastructure.Data.Context;

namespace Nemui.Api.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options => 
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        
        return services;
    }
} 