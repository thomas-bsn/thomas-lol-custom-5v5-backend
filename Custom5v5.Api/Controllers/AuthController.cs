using System.Security.Claims;
using Custom5v5.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Custom5v5.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IOAuthStateStore _stateStore;
    private readonly IDiscordOAuthClient _discord;
    private readonly IJwtIssuer _jwt;

    public AuthController(
        IConfiguration config,
        IOAuthStateStore stateStore,
        IDiscordOAuthClient discord,
        IJwtIssuer jwt)
    {
        _config = config;
        _stateStore = stateStore;
        _discord = discord;
        _jwt = jwt;
    }

    // 1) Redirect vers Discord OAuth2 authorize
    // GET /auth/discord/login?returnUrl=http://localhost:3000/auth/callback
    [HttpGet("discord/login")]
    [AllowAnonymous]
    public IActionResult DiscordLogin([FromQuery] string? returnUrl = null)
    {
        var clientId = _config["Auth:Discord:ClientId"];
        var redirectUri = _config["Auth:Discord:RedirectUri"];

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(redirectUri))
            return Problem("Missing Auth:Discord:ClientId or Auth:Discord:RedirectUri in configuration.");

        // state = anti-CSRF + anti-replay
        var state = _stateStore.CreateState(returnUrl);

        // IMPORTANT: ajoute "guilds" sinon /users/@me/guilds ne marche pas
        var scope = "identify guilds";

        var authorizeUrl =
            "https://discord.com/api/oauth2/authorize" +
            $"?client_id={Uri.EscapeDataString(clientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            "&response_type=code" +
            $"&scope={Uri.EscapeDataString(scope)}" +
            $"&state={Uri.EscapeDataString(state)}";

        return Redirect(authorizeUrl);
    }

    // 2) Callback Discord -> échange code -> /users/@me -> JWT
    // GET /auth/discord/callback?code=...&state=...
    [HttpGet("discord/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> DiscordCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(new { error = "missing_code" });

        if (string.IsNullOrWhiteSpace(state))
            return BadRequest(new { error = "missing_state" });

        var stateData = _stateStore.ConsumeState(state);
        if (stateData is null)
            return Unauthorized(new { error = "invalid_state" });

        var allowedGuildId = _config["Auth:Discord:AllowedGuildId"];
        if (string.IsNullOrWhiteSpace(allowedGuildId))
            throw new InvalidOperationException("Missing Auth:Discord:AllowedGuildId configuration.");

        // 1) échange code -> access_token
        var token = await _discord.ExchangeCodeAsync(code, ct);

        if (string.IsNullOrWhiteSpace(token.AccessToken))
            throw new InvalidOperationException(
                $"Discord returned empty access_token. Raw response: {JsonSerializer.Serialize(token)}"
            );

        // 2) check guild membership (bloque avant d'émettre un JWT)
        var isMember = await _discord.IsMemberOfGuildAsync(token.AccessToken, allowedGuildId, ct);
        if (!isMember)
            return StatusCode(403, new { error = "not_in_guild" });

        // 3) appelle Discord /users/@me
        var me = await _discord.GetCurrentUserAsync(token.AccessToken, ct);

        // 4) émettre JWT (sub = discord user id)
        var jwt = _jwt.Issue(new JwtUser(
            DiscordUserId: me.Id,
            Username: me.Username,
            Discriminator: me.Discriminator
        ));

        // MVP : on renvoie JSON, le front stocke le token
        return Ok(new
        {
            token = jwt.AccessToken,
            expiresAtUtc = jwt.ExpiresAtUtc,
            user = new
            {
                discordUserId = me.Id,
                username = me.Username,
                discriminator = me.Discriminator
            },
            returnUrl = stateData.ReturnUrl
        });
    }

    // 3) Who am I (JWT required)
    // GET /auth/me
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var discordUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var username = User.FindFirstValue(ClaimTypes.Name);

        return Ok(new
        {
            discordUserId,
            username
        });
    }

    // 4) Logout (JWT stateless => rien à faire côté serveur)
    // POST /auth/logout
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        // Avec JWT stateless, "logout" = client delete token.
        // Si tu veux une vraie révocation: blacklist jti en DB/redis (pas MVP).
        return Ok(new { ok = true });
    }
}




