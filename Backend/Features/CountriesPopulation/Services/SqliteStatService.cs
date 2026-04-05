using Backend.Features.CountriesPopulation.Common;
using Backend.Features.CountriesPopulation.Interfaces;
using Backend.Infrastructure.Interfaces;
using Dapper;

namespace Backend.Features.CountriesPopulation.Services;

public class SqliteStatService(
    IDbManager dbManager
) : IStatService
{
    private const string Query = """
    SELECT
        c.[CountryName],
        CAST(a.[Population] AS INT) AS [Population]
    FROM
    (
        SELECT
            c.[CountryId],
            SUM(cy.[Population]) AS [Population]
        FROM [Country] c
        INNER JOIN [State] s
            ON s.[CountryId] = c.[CountryId]
        INNER JOIN [City] cy
            ON cy.[StateId] = s.[StateId]
        GROUP BY c.[CountryId]
    ) a
    INNER JOIN [Country] c 
        ON c.[CountryId] = a.[CountryId]
    """;
    
    public async Task<List<CountryPopulationRecord>> GetCountryPopulationsAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var connection = dbManager.GetConnection();
        var results = await connection.QueryAsync<CountryPopulationRecord>(Query, cancellationToken);
        return [.. results];
    }

    public string ServiceType => "SqliteCountryDataSource";
}