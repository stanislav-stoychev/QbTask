using Backend.Features.CountriesPopulation.Common;
using Backend.Features.CountriesPopulation.Interfaces;

namespace Backend.Features.CountriesPopulation.Services;

public class ConcreteStatService : IStatService
{
    private static List<CountryPopulationRecord> GetCountryPopulations()
    {
        // Pretend this calls a REST API somewhere
        return
        [
			new CountryPopulationRecord("India",1182105000),
			new CountryPopulationRecord("United Kingdom",62026962),
		    new CountryPopulationRecord("Chile",17094270),
		    new CountryPopulationRecord("Mali",15370000),
		    new CountryPopulationRecord("Greece",11305118),
		    new CountryPopulationRecord("Armenia",3249482),
		    new CountryPopulationRecord("Slovenia",2046976),
		    new CountryPopulationRecord("Saint Vincent and the Grenadines",109284),
		    new CountryPopulationRecord("Bhutan",695822),
		    new CountryPopulationRecord("Aruba (Netherlands)",101484),
		    new CountryPopulationRecord("Maldives",319738),
		    new CountryPopulationRecord("Mayotte (France)",202000),
		    new CountryPopulationRecord("Vietnam",86932500),
		    new CountryPopulationRecord("Germany",81802257),
		    new CountryPopulationRecord("Botswana",2029307),
		    new CountryPopulationRecord("Togo",6191155),
		    new CountryPopulationRecord("Luxembourg",502066),
		    new CountryPopulationRecord("U.S. Virgin Islands (US)",106267),
		    new CountryPopulationRecord("Belarus",9480178),
		    new CountryPopulationRecord("Myanmar",59780000),
		    new CountryPopulationRecord("Mauritania",3217383),
		    new CountryPopulationRecord("Malaysia",28334135),
		    new CountryPopulationRecord("Dominican Republic",9884371),
		    new CountryPopulationRecord("New Caledonia (France)",248000),
		    new CountryPopulationRecord("Slovakia",5424925),
		    new CountryPopulationRecord("Kyrgyzstan",5418300),
			new CountryPopulationRecord("Lithuania",3329039),
			new CountryPopulationRecord("United States of America",309349689)
        ];
    }


    public Task<List<CountryPopulationRecord>> GetCountryPopulationsAsync(
		CancellationToken cancellationToken = default
	) => Task.FromResult(GetCountryPopulations());

	public string ServiceType => "HttpCountryDataSource";
}
