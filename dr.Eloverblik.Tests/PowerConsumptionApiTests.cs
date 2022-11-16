using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace dr.Eloverblik.Tests;

public class PowerConsumptionApiTests
{
    private PowerConsumptionApi _sut;
    private MeteringPoint _meteringPoint = null; 
    [SetUp]
    public async Task Setup()
    {
        var token = Environment.GetEnvironmentVariable("ELOVERBLIK_TOKEN");
        Assert.IsNotNull(token, "Missing token ELOVERBLIK_TOKEN in environment.");
        ApiOptions options = new ApiOptions(new BearerToken(token), new Uri("https://api.eloverblik.dk"));
        var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        _sut = new PowerConsumptionApi(options, new HttpClient(), loggerFactory.CreateLogger<PowerConsumptionApi>());
        if (_meteringPoint == null)
        {
            var mp = await _sut.GetMeteringPoints(CancellationToken.None);
            _meteringPoint = mp.Single();    
        }
    }

    [Test]
    public async Task CanGetMeteringPoints()
    {
        var mp = await _sut.GetMeteringPoints(default);
        Assert.That(mp.Count > 0);
    }

    [Test]
    public async Task CanGetTimeSeries()
    {
        var series = await _sut.GetConsumptionTimeSeries(_meteringPoint, new DateOnly(2022, 10, 1), new DateOnly(2022, 11, 1),
            PowerMeasurementAggregation.Day, default);

        Assert.That(series.Count, Is.EqualTo(31));
    }

    [Test]
    public async Task CanGetTimeSeries_Hourly()
    {
        var series = await _sut.GetConsumptionTimeSeries(_meteringPoint, new DateOnly(2022, 10, 1), new DateOnly(2022, 10, 3),
            PowerMeasurementAggregation.Hour, default);

        Assert.That(series.Count, Is.EqualTo(48));
        Assert.That(series.Select(x => x.When.Start), Is.Ordered.Ascending);
    }
}