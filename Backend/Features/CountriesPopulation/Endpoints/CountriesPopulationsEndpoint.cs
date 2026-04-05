using Backend.Features.CountriesPopulation.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Authorization;

namespace Backend.Features.CountriesPopulation.Endpoints;

[HttpGet("/api/countries-population")]
[AllowAnonymous]
public class CountryPopulationsEndpoint(
    IDataAggregationPipeline aggregationPipeline
) : EndpointWithoutRequest<CountryPopulationsResponse>
{
    public override async Task HandleAsync(CancellationToken token)
    {
        var result = await aggregationPipeline.Aggregate(token);
        if(result is null)
        {
            AddError("System failed to perform the request. Please check logs.");
            await SendErrorsAsync(statusCode: 500, cancellation: token);
            return;
        }

        Response = new()
        {
            Items = result
        };
    }
}