using Backend.Infrastructure.Configuration;
using Backend.Infrastructure.Interfaces;
using Backend.Infrastructure.Services;

namespace Backend.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection BootstrapApp(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddOptions<DatabaseConfiguration>()
            .Bind(configuration.GetSection(nameof(DatabaseConfiguration)))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services
            .AddSingleton<IDbManager, SqliteDbManager>();
    }
}