using Backend.Features.CountriesPopulation.Extensions;
using Backend.Infrastructure.Extensions;
using FastEndpoints;
using FastEndpoints.Swagger;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddFastEndpoints()
    .SwaggerDocument()
    .BootstrapApp(builder.Configuration)
    .AddCountryPopulationServices(builder.Configuration);

var app = builder.Build();
app.UseFastEndpoints();
app.UseDefaultExceptionHandler();

if(app.Environment.IsDevelopment())
{
    app.UseSwaggerGen();   
}

app.Run();