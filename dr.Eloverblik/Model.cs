using System.Text.Json.Serialization;

namespace dr.Eloverblik;

record AuthenticationResult(string Result);
record ResultWrapper<T>(IReadOnlyCollection<T> Result);


record PowerMeasurementTimeSeries(IReadOnlyCollection<PowerMeasurementRecord> Period);
record PowerMeasurementInner(IReadOnlyCollection<PowerMeasurementTimeSeries> TimeSeries);
record PowerMeasurementRoot(bool Success, [property: JsonPropertyName("MyEnergyData_MarketDocument")] PowerMeasurementInner Inner);
record PowerMeasurementRecord(string Resolution, TimeIntervalDto TimeInterval, IReadOnlyCollection<PowerMeasurementValue> Point);

record PowerMeasurementValue(string Position, 
    [property: JsonPropertyName("out_Quantity.quantity")] string? Quantity, 
    [property: JsonPropertyName("out_Quantity.quality")] string? Quality);

record TimeIntervalDto(DateTimeOffset Start, DateTimeOffset End);

// Public model
public record MeteringPoint(string MeteringPointId, string BalanceSupplierName, string PostCode, string StreetName, string BuildingNumber);
public record PowerMeasurement(TimeInverval When, PowerMeasurementAggregation Aggregation,
    decimal Quantity, string Quality);

public record TimeInverval(DateTimeOffset Start, DateTimeOffset End)
{
    public override string ToString() => $"{Start} -> {End}";
};

public enum PowerMeasurementAggregation
{
    Actual, Quarter, Hour, Day, Month, Year
}
