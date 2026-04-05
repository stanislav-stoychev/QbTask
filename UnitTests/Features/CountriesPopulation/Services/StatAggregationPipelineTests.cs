using Backend.Features.CountriesPopulation.Common;
using Backend.Features.CountriesPopulation.Configurations;
using Backend.Features.CountriesPopulation.Interfaces;
using Backend.Features.CountriesPopulation.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Polly;
using Polly.Registry;

namespace UnitTests.Features.CountriesPopulation.Services;

public class StatAggregationPipelineTests
{
    private readonly IEnumerable<IStatService> _statServicesMocks;
    private readonly IStatService _dbMock;
    private readonly IStatService _httpMock;
    private readonly IOptionsMonitor<StatAggregationConfiguration> _configMock;
    private readonly ResiliencePipelineProvider<string> _pipelineProviderMock;
    private readonly ILogger<StatAggregationPipeline> _loggerMock;
    private readonly StatAggregationPipeline _sytemUnderTest;

    public StatAggregationPipelineTests()
    {
        _dbMock = Substitute.For<IStatService>();
        _httpMock = Substitute.For<IStatService>();
        _statServicesMocks = [_dbMock, _httpMock];
        _configMock = Substitute.For<IOptionsMonitor<StatAggregationConfiguration>>();
        _pipelineProviderMock = Substitute.For<ResiliencePipelineProvider<string>>();
        _loggerMock = Substitute.For<ILogger<StatAggregationPipeline>>();

        _pipelineProviderMock.GetPipeline(nameof(StatAggregationPipeline))
            .Returns(ResiliencePipeline.Empty);
        
        _sytemUnderTest = new(_statServicesMocks, _configMock, _pipelineProviderMock, _loggerMock);
    }

    [Fact]
    public async Task Aggregate_combines_data_by_all_match_types()
    {
        // Arrange
        _dbMock.ServiceType.Returns("DbService");
        _dbMock.GetCountryPopulationsAsync(CancellationToken.None)
            .Returns([
                // Alias match
                new CountryPopulationRecord("USA", 111),
                // Junk match
                new CountryPopulationRecord("Democratic republic of Congo", 112),
                // Direct match
                new CountryPopulationRecord("Canada", 113),
                // No match
                new CountryPopulationRecord("Bulgaria", 114),
            ]);

        _httpMock.ServiceType.Returns("HttpService");
        _httpMock.GetCountryPopulationsAsync(CancellationToken.None)
            .Returns([
                // Alias match
                new CountryPopulationRecord("united states", 211),
                // Junk match
                new CountryPopulationRecord("congo", 212),
                // Direct match
                new CountryPopulationRecord("canada", 213),
                // No match
                new CountryPopulationRecord("Serbia", 214),
            ]);

        _configMock.CurrentValue.Returns(new StatAggregationConfiguration
        {
            StatSourcesPresedence = [
                new()
                {
                    Name = "DbService",
                    Order = 0
                },
                new()
                {
                    Name = "HttpService",
                    Order = 1
                }
            ],
            RetrySettings = new()
            {
                DelayInSeconds = 1,
                NumberOfRetries = 1,
                TimeoutInSeconds = 60
            },
            AliasMatrice = [
                new()
                {
                    Aliases = ["USA", "United States"]
                }
            ],
            Junk = ["\\.", ",", "\\bof\\b", "\\bthe\\b", "\\brepublic\\b", "\\bdemocratic\\b", "\\(([^)]+)\\)"]
        });
        
        // Act
        var result = await _sytemUnderTest.Aggregate(CancellationToken.None);
        var expectedResult = new List<dynamic>
        {
            new
            {
                name = "USA",
                population = 111
            },
            new
            {
                name = "Democratic republic of Congo",
                population = 112
            },
            new
            {
                name = "Canada",
                population = 113
            },
            new
            {
                name = "Bulgaria",
                population = 114
            },
            new
            {
                name = "Serbia",
                population = 214
            }
        };

        // Assert
        Assert.NotNull(result!);
        Assert.Equal(5, result.Length);
        foreach(var res in expectedResult)
        {
            var match = result.Single(r => r.CountryName == res.name);
            Assert.Equal(res.name, match.CountryName);
            Assert.Equal(res.population, match.Population);
        }
    }

    [Fact]
    public async Task Aggregate_handles_when_data_source_fails_to_yeld_data()
    {
        // Arrange
        _dbMock.ServiceType.Returns("DbService");
        _dbMock.GetCountryPopulationsAsync(CancellationToken.None)
            .Returns([
                // Alias match
                new CountryPopulationRecord("USA", 111),
                // Junk match
                new CountryPopulationRecord("Democratic republic of Congo", 112),
                // Direct match
                new CountryPopulationRecord("Canada", 113),
                // No match
                new CountryPopulationRecord("Bulgaria", 114),
            ]);

        _httpMock.ServiceType.Returns("HttpService");
        _httpMock.GetCountryPopulationsAsync(CancellationToken.None)
            .Throws(new Exception("Error"));

        _configMock.CurrentValue.Returns(new StatAggregationConfiguration
        {
            StatSourcesPresedence = [
                new()
                {
                    Name = "DbService",
                    Order = 0
                },
                new()
                {
                    Name = "HttpService",
                    Order = 1
                }
            ],
            RetrySettings = new()
            {
                DelayInSeconds = 1,
                NumberOfRetries = 1,
                TimeoutInSeconds = 60
            },
            AliasMatrice = [
                new()
                {
                    Aliases = ["USA", "United States"]
                }
            ],
            Junk = ["\\.", ",", "\\bof\\b", "\\bthe\\b", "\\brepublic\\b", "\\bdemocratic\\b", "\\(([^)]+)\\)"]
        });
        
        // Act
        var result = await _sytemUnderTest.Aggregate(CancellationToken.None);
        var expectedResult = new List<dynamic>
        {
            new
            {
                name = "USA",
                population = 111
            },
            new
            {
                name = "Democratic republic of Congo",
                population = 112
            },
            new
            {
                name = "Canada",
                population = 113
            },
            new
            {
                name = "Bulgaria",
                population = 114
            }
        };

        // Assert
        Assert.NotNull(result!);
        Assert.Equal(4, result.Length);
        foreach(var res in expectedResult)
        {
            var match = result.Single(r => r.CountryName == res.name);
            Assert.Equal(res.name, match.CountryName);
            Assert.Equal(res.population, match.Population);
        }
    }

    [Fact]
    public async Task Aggregate_returns_null_when_all_data_sources_fail_to_yeld_data()
    {
        // Arrange
        _dbMock.ServiceType.Returns("DbService");
        _dbMock.GetCountryPopulationsAsync(CancellationToken.None)
            .Throws(new Exception("Error"));

        _httpMock.ServiceType.Returns("HttpService");
        _httpMock.GetCountryPopulationsAsync(CancellationToken.None)
            .Throws(new Exception("Error"));

        _configMock.CurrentValue.Returns(new StatAggregationConfiguration
        {
            StatSourcesPresedence = [
                new()
                {
                    Name = "DbService",
                    Order = 0
                },
                new()
                {
                    Name = "HttpService",
                    Order = 1
                }
            ],
            RetrySettings = new()
            {
                DelayInSeconds = 1,
                NumberOfRetries = 1,
                TimeoutInSeconds = 60
            },
            AliasMatrice = [
                new()
                {
                    Aliases = ["USA", "United States"]
                }
            ],
            Junk = ["\\.", ",", "\\bof\\b", "\\bthe\\b", "\\brepublic\\b", "\\bdemocratic\\b", "\\(([^)]+)\\)"]
        });
        
        // Act
        var result = await _sytemUnderTest.Aggregate(CancellationToken.None);

        // Assert
        Assert.Null(result);
    }
}