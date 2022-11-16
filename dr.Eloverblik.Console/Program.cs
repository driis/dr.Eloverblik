using dr.Eloverblik;
using Microsoft.Extensions.Logging;
using static System.Console;
if (args.Length < 1)
{
    WriteLine("Eloverblik.dk API refresh token is expected in first parameter");
    return;
}

string token = args[0];
var options = new ApiOptions(
    new BearerToken(token), 
    new Uri("https://api.eloverblik.dk"));
var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
var api = new PowerConsumptionApi(options, new HttpClient(), loggerFactory.CreateLogger<PowerConsumptionApi>());
CancellationToken ct = CancellationToken.None;
var meteringPoints = await api.GetMeteringPoints(ct);
WriteLine("The following metering points is available to the token provided:");
foreach (var mp in meteringPoints)
{
    WriteLine(mp);
}
if (meteringPoints.Count < 1)
    return;

var point = meteringPoints.First();
DateOnly begin  = new DateOnly(DateTime.Now.Year, DateTime.Now.Month - 3, 1);
DateOnly end = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
WriteLine($"Continuing with first metering point - getting consumption between {begin} and {end}.");
var consumption = await api.GetConsumptionTimeSeries(point, begin,end, PowerMeasurementAggregation.Hour, ct);

var lines = new[] {"Start,End,QuantityKwh,Aggregation"}
    .Concat(consumption.Select(c => String.Join(',', c.When.Start, c.When.end, c.Quantity, c.Aggregation)));

File.WriteAllLines("output.csv", lines);