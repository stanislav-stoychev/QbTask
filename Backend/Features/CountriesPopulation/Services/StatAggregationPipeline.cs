using System.Text.RegularExpressions;
using Backend.Features.CountriesPopulation.Common;
using Backend.Features.CountriesPopulation.Configurations;
using Backend.Features.CountriesPopulation.Interfaces;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;

namespace Backend.Features.CountriesPopulation.Services;

public partial class StatAggregationPipeline(
    IEnumerable<IStatService> statServices,
    IOptionsMonitor<StatAggregationConfiguration> config,
    ResiliencePipelineProvider<string> pipelineProvider,
    ILogger<StatAggregationPipeline> logger
) : IDataAggregationPipeline
{
    public async Task<CountryPopulationRecord[]?> Aggregate(
        CancellationToken cancellationToken
    )
    {
        var statServices = GetStatServicesByPrecedence();
        var pipeline = pipelineProvider.GetPipeline(nameof(StatAggregationPipeline));
        var tasks = statServices.Select(s => RunQuery(pipeline, s, cancellationToken))
            .ToArray();

        // Now we have aggregates ordered by precedence
        var aggregatedRuslts = await Task.WhenAll(tasks);
        // If all task failed return null so the caller can react on erros
        if(aggregatedRuslts.All(t => !t.success))
        {
            return null;
        }
    
        return Merge([.. aggregatedRuslts.Select(a => a.result).Where(a => a.Count != 0)]);
    }

    private async Task<(List<CountryPopulationRecord> result, bool success)> RunQuery(
        ResiliencePipeline pipeline,
        IStatService statService,
        CancellationToken cancellationToken
    )
    {
        try
        {  
            return await pipeline.ExecuteAsync(async token => 
            {
                return (await statService.GetCountryPopulationsAsync(token), true);
            }, cancellationToken);
        }
        catch(Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to load country data from source {FailedStatService}",
                statService.GetType().Name
            );

            // Even if one data source fails to provide we proceed with the pipeline and eventually return data
            return ([], false);
        }
    }

    private IStatService[] GetStatServicesByPrecedence()
    {
        var order = config.CurrentValue.StatSourcesPresedence
            .OrderBy(p => p.Order)
            .ToArray();

        var result = new IStatService[statServices.Count()];
        for(var i = 0; i < result.Length; i++)
        {
            result[i] = statServices.Single(s => s.ServiceType == order[i].Name);
        }

        return result;
    }

    private CountryPopulationRecord[] Merge(
        List<CountryPopulationRecord>[] aggregates
    )
    {
        if(aggregates.Length == 0)
        {
            return [];
        }

        // Get the aggregate which has the highest precedence (Db source in our case)
        var result = aggregates.First();
        if(aggregates.Length == 1)
        {
            return [..result.OrderBy(r => r.CountryName)];
        }
        
        for(var i = 1; i < aggregates.Length; i++)
        {
            foreach(var country in aggregates.ElementAt(i))
            {
                if(country is null)
                {
                    continue;
                }
                
                if(Match(result, country.CountryName!))
                {
                    continue;
                }

                result.Add(country);
            }
        }

        return [..result.OrderBy(r => r.CountryName)];
    }

    private bool Match(
        IEnumerable<CountryPopulationRecord> result,
        string currentCountry
    )
    {
        // Match directly (First because there is no chance for false positives)
        // Match using alias vectror / Skipped when settings not set (Second because it could give false positives if the settings are not well tailored)
        // Match with junk removal / Skipped when settings not set (Last because its a bit undeterministic and expensive due to regex)      
        
        // These could be separate IEnumerable dependency similar to statServices but IMO looks like an overkill
        var matchPipe = new List<Func<IEnumerable<CountryPopulationRecord>, string, bool>>
        {
            DirectMatch, MatchFromAliasMatrice, MatchWithoutJunk
        };
        
        return matchPipe.Any(f => f(result, currentCountry));
    }

    private static bool DirectMatch(
        IEnumerable<CountryPopulationRecord> result,
        string currentCountry
    ) => result.Any(r => string.Compare(r.CountryName, currentCountry, true) == 0);

    private bool MatchFromAliasMatrice(
        IEnumerable<CountryPopulationRecord> result,
        string currentCountry
    )
    {
        var aliasMatrice = config.CurrentValue.AliasMatrice;
        if((aliasMatrice?.Length ?? 0) == 0)
        {
            return false;
        }

        foreach(var vector in aliasMatrice!)
        {
            if((vector.Aliases?.Length ?? 0) == 0)
            {
                continue;
            }

            if(vector.Aliases!.Any(a => string.Compare(a, currentCountry.ToLower(), true) == 0))
            {
                var match = result.Select(r => r.CountryName!.ToLower())
                    .Join(vector.Aliases!,
                        r => new { Name = r.ToLower()! },
                        l => new { Name = l.ToLower() },
                        (r, l) => ""
                    );
                
                if(match?.Any() ?? false)
                {
                    LogAliasMatchFound(logger, currentCountry);
                    return true; 
                }
            }
        }

        LogAliasMatchNotFound(logger, currentCountry);
        return false;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Found country match by alias for {CountryNameAliasFound}")]
    static partial void LogAliasMatchFound(ILogger logger, string countryNameAliasFound);

    [LoggerMessage(Level = LogLevel.Information, Message = "Country match not found by alias for {CountryNameAliasNotFound}")]
    static partial void LogAliasMatchNotFound(ILogger logger, string countryNameAliasNotFound);

    private bool MatchWithoutJunk(
        IEnumerable<CountryPopulationRecord> result,
        string currentCountry
    )
    {
        var junk = config.CurrentValue.Junk;
        if((junk?.Length ?? 0) == 0)
        {
            return false;
        }

        var pattern = string.Join("|", junk!);
        var escapedCountry = Regex.Replace(
            currentCountry, pattern, "",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        )?.Trim();
        
        foreach(var country in result)
        {
            var currentEscaped = Regex.Replace(
                country.CountryName!, pattern, "",
                RegexOptions.IgnoreCase | RegexOptions.Compiled
            )?.Trim();

            if(string.Compare(escapedCountry, currentEscaped, true) == 0)
            {
                LogJunkRemovalMatchFound(logger, currentCountry);
                return true;
            }
        }

        LogJunkRemovalMatchNotFound(logger, currentCountry);
        return false;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Found country match by junk removal for {CountryNameJunkFound}")]
    static partial void LogJunkRemovalMatchFound(ILogger logger, string countryNameJunkFound);

    [LoggerMessage(Level = LogLevel.Information, Message = "Country match not found by junk removal for {CountryNameJunkNotFound}")]
    static partial void LogJunkRemovalMatchNotFound(ILogger logger, string countryNameJunkNotFound);
}