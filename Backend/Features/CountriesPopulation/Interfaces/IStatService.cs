using Backend.Features.CountriesPopulation.Common;

namespace Backend.Features.CountriesPopulation.Interfaces;

public interface IStatService
{
    Task<List<CountryPopulationRecord>> GetCountryPopulationsAsync(CancellationToken cancellationToken = default);

    public abstract string ServiceType { get; }
}