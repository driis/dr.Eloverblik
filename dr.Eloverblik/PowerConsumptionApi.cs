using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace dr.Eloverblik;

public class PowerConsumptionApi
{
    private readonly HttpClient _client;
    private readonly ILogger<PowerConsumptionApi> _logger;
    private readonly ApiOptions _options;
    private AuthenticationHeaderValue? _authToken;

    public PowerConsumptionApi(ApiOptions options, HttpClient client, ILogger<PowerConsumptionApi> logger)
    {
        _client = client;
        _client.BaseAddress = options.BaseUri;
        _client.DefaultRequestHeaders.Accept.Clear();
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _options = options;
        _logger = logger;
    }
    
    public async Task Authenticate()
    {
        _authToken = await AuthenticateInner();
    }

    public async Task<IReadOnlyCollection<MeteringPoint>> GetMeteringPoints(CancellationToken ct)
    {
        HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Get, "/customerapi/api/meteringpoints/meteringpoints");
        var meteringPoints = await CallApi<ResultWrapper<MeteringPoint>>(msg, ct);
        return meteringPoints.Result;
    }

    public async Task<IReadOnlyCollection<PowerMeasurement>> GetConsumptionTimeSeries(
        MeteringPoint mp, 
        DateOnly from,
        DateOnly to, 
        PowerMeasurementAggregation aggregation, 
        CancellationToken ct)
    {
        HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Post,
            $"/customerapi/api/meterdata/gettimeseries/{from:yyyy-MM-dd}/{to:yyyy-MM-dd}/{aggregation}");
        msg.Content = JsonContent.Create(new {meteringPoints = new {meteringPoint = new[] {mp.MeteringPointId}}});
        var result = await CallApi<ResultWrapper<PowerMeasurementRoot>>(msg, ct);

        var records = result.Result.FirstOrDefault()?.Inner.TimeSeries.FirstOrDefault()?.Period;
        if (records == null)
            throw new ApplicationException($"Invalid data returned from {msg.RequestUri}");

        var ts = records.SelectMany(x => MapTimeSeriesRecord(x, aggregation));
        return ts.ToList();
    }

    private IEnumerable<PowerMeasurement> MapTimeSeriesRecord(PowerMeasurementRecord value, PowerMeasurementAggregation aggregation)
    {
        DateTimeOffset start = value.TimeInterval.Start;
        DateTimeOffset end = value.TimeInterval.End;
        TimeSpan increment = aggregation switch
        {
            PowerMeasurementAggregation.Hour => TimeSpan.FromHours(1),
            _ => end - start
        };
        return value.Point.Select(point =>
        {
            if (!Decimal.TryParse(point.Quantity, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal quantity))
                throw new ApplicationException($"Parsing values failed: {point}");

            end = start + increment;
            var interval = new TimeInverval(start, end);
            start = end;
            return new PowerMeasurement(interval, aggregation, quantity, point.Quality ?? String.Empty);
        });
    }

    private async Task<AuthenticationHeaderValue> AuthToken()
    {
        if (_authToken == null)
        {
            _authToken = await AuthenticateInner();
        }

        return _authToken;
    }
    
    private async Task<T> CallApi<T>(HttpRequestMessage msg, CancellationToken ct)
    {
        msg.Headers.Authorization = await AuthToken();
        var response = await _client.SendAsync(msg, ct);
        response.EnsureSuccessStatusCode();
        T? result = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
        if (result == null)
        {
            throw new ApplicationException($"Got {response.StatusCode}, but result did not deserialize into expected type {typeof(T)}");
        }

        return result;
    }
    private async Task<AuthenticationHeaderValue> AuthenticateInner()
    {
        HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Get, "/customerapi/api/token"); 
        msg.Headers.Authorization = new AuthenticationHeaderValue(BearerToken.Scheme, _options.RefreshToken);
        var response = await _client.SendAsync(msg);
        response.EnsureSuccessStatusCode();
        var authResult = await response.Content.ReadFromJsonAsync<AuthenticationResult>();
        if (authResult?.Result == null)
        {
            throw new ApplicationException("Authenticated with success status code, but body was unexpected");
        }

        return new AuthenticationHeaderValue("Bearer", authResult.Result);
    }
}