using Backend.Features.CountriesPopulation.Common;

namespace Backend.Features.CountriesPopulation.Endpoints;

public class CountryPopulationsResponse
{
    public required CountryPopulationRecord[] Items { get; set; }
}