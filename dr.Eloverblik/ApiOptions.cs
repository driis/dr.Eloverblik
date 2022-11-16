namespace dr.Eloverblik;

public record ApiOptions(BearerToken RefreshToken, Uri BaseUri);


public record BearerToken(string Value)
{
    public const string Scheme = "Bearer";
    public static implicit operator string(BearerToken token) => token.Value;
}