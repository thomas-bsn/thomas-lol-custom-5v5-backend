namespace Custom5v5.Infrastructure.Options;

public sealed class DiscordOAuthOptions
{
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; } // SECRET
    public required string RedirectUri { get; init; }
    public string BaseUrl { get; init; } = "https://discord.com/api/";
}