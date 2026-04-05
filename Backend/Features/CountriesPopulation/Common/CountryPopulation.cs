namespace Backend.Features.CountriesPopulation.Common;

public record CountryPopulationRecord
{
    public string? CountryName { get; init; }
    
    public int Population { get; init; }

    public CountryPopulationRecord(string countryName, int population)
    {
        CountryName = countryName;
        Population = population;
    }

    public CountryPopulationRecord() { }
}