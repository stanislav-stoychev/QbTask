using System.Reflection;
using Backend.Features.CountriesPopulation.Configurations;
using Backend.Features.CountriesPopulation.Interfaces;
using Microsoft.Extensions.Options;

namespace Backend.Features.CountriesPopulation.Validators;

public class ConfigurationValidator(
    IEnumerable<IStatService> statServices
) : IValidateOptions<StatAggregationConfiguration>
{
    public ValidateOptionsResult Validate(
        string? name,
        StatAggregationConfiguration options
    )
    {
        if(options.StatSourcesPresedence.Length == 0)
        {
            return ValidateOptionsResult.Fail("Precedence must be set.");
        }

        var existingServices = statServices
            .Select(t => t.ServiceType)
            .ToArray();

        var length = options.StatSourcesPresedence.Length;
        if(length != existingServices.Length)
        {
            return ValidateOptionsResult.Fail("Precedence list does not reflect registered services.");
        }

        var ordered = options.StatSourcesPresedence.OrderBy(p => p.Order)
            .ToArray();

        for(var i = 0; i < length; i++)
        {
            if(i != ordered[i].Order)
            {
                return ValidateOptionsResult.Fail("Incorrect precedence is set.");    
            }

            if(!existingServices.Any(s => s == ordered[i].Name))
            {
                return ValidateOptionsResult.Fail("Precedence list does not reflect registered services.");    
            }
        }

        return ValidateOptionsResult.Success;
    }
}