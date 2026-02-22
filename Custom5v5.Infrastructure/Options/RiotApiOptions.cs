namespace Custom5v5.Infrastructure.Options;

public sealed class RiotApiOptions
{
    public required string ApiKey { get; init; }
    public string BaseUrl { get; init; } = "https://europe.api.riotgames.com";
}