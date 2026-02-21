using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Custom5v5.Api.Interfaces;

namespace Custom5v5.Api.Services;

public sealed class DiscordOAuthClient : IDiscordOAuthClient
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public DiscordOAuthClient(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    // 1) Exchange OAuth code -> access_token
    public async Task<DiscordTokenResponse> ExchangeCodeAsync(string code, CancellationToken ct)
    {
        var clientId = _config["Auth:Discord:ClientId"];
        var clientSecret = _config["Auth:Discord:ClientSecret"];
        var redirectUri = _config["Auth:Discord:RedirectUri"];

        if (string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(clientSecret) ||
            string.IsNullOrWhiteSpace(redirectUri))
        {
            throw new InvalidOperationException("Missing Discord OAuth configuration.");
        }

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri
        });

        using var response = await _http.PostAsync(
            "https://discord.com/api/oauth2/token",
            content,
            ct
        );

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"Discord token exchange failed ({(int)response.StatusCode}): {body}"
            );
        }

        var json = await response.Content.ReadAsStringAsync(ct);

        var token = JsonSerializer.Deserialize<DiscordTokenResponse>(json)
                    ?? throw new InvalidOperationException("Invalid token response from Discord.");

        return token;
    }

    // 2) Call /users/@me
    public async Task<DiscordMeResponse> GetCurrentUserAsync(string accessToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "https://discord.com/api/users/@me"
        );

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _http.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"Discord /users/@me failed ({(int)response.StatusCode}): {body}"
            );
        }

        var json = await response.Content.ReadAsStringAsync(ct);

        var me = JsonSerializer.Deserialize<DiscordMeResponse>(json)
                 ?? throw new InvalidOperationException("Invalid /users/@me response from Discord.");

        return me;
    }
}