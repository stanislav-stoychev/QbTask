using Backend.Features.CountriesPopulation.Configurations;
using Backend.Features.CountriesPopulation.Interfaces;
using Backend.Features.CountriesPopulation.Services;
using Backend.Features.CountriesPopulation.Validators;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace Backend.Features.CountriesPopulation.Extensions;

public static class CountryPopulationExtensions
{
    public static IServiceCollection AddCountryPopulationServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {   
        services
            .AddOptions<StatAggregationConfiguration>()
            .Bind(configuration.GetSection(nameof(StatAggregationConfiguration)))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services.Scan(s => 
            s.FromEntryAssembly()
                .AddClasses(c => c.AssignableTo<IStatService>())
                .AsSelfWithInterfaces()
                .WithSingletonLifetime()
        )
        .AddResiliencePipeline(nameof(StatAggregationPipeline), (builder, ctx) =>
        {
            var cfg = ctx.ServiceProvider.GetRequiredService<IOptionsMonitor<StatAggregationConfiguration>>().CurrentValue;

            builder
                .AddRetry(new RetryStrategyOptions
                {
                    Delay = TimeSpan.FromSeconds(cfg.RetrySettings.TimeoutInSeconds),
                    MaxRetryAttempts = cfg.RetrySettings.NumberOfRetries,
                    BackoffType = DelayBackoffType.Exponential
                })
                .AddTimeout(TimeSpan.FromSeconds(cfg.RetrySettings.TimeoutInSeconds));
        })
        .AddSingleton<IDataAggregationPipeline, StatAggregationPipeline>()
        .AddSingleton<IValidateOptions<StatAggregationConfiguration>, ConfigurationValidator>();
    }
}