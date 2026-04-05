using Backend.Features.CountriesPopulation.Common;

namespace Backend.Features.CountriesPopulation.Interfaces;

public interface IDataAggregationPipeline
{
    Task<CountryPopulationRecord[]?> Aggregate(CancellationToken cancellationToken);
}