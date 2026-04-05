using System.ComponentModel.DataAnnotations;

namespace Backend.Features.CountriesPopulation.Configurations;

public class PrecedenceDescriptor
{
    public int Order { get; set; }

    [Required]
    public required string Name { get; set; }    
}

public class AliasVector
{
    public string[] Aliases { get; set; } = [];
}

public class RetrySettings
{
    public int NumberOfRetries { get; set; }

    public int DelayInSeconds { get; set; }

    public int TimeoutInSeconds { get; set; }
}

public class StatAggregationConfiguration
{
    [Required]
    public required PrecedenceDescriptor[] StatSourcesPresedence { get; set; } = [];

    public AliasVector[] AliasMatrice { get; set; } = [];

    public string[] Junk { get; set; } = [];

    [Required]
    public required RetrySettings RetrySettings { get; set; }
}