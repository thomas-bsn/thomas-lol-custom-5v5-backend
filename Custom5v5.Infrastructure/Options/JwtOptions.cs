namespace Custom5v5.Infrastructure.Options;

public sealed class JwtOptions
{
    public required string Issuer { get; init; }
    public required string Audience { get; init; }

    // Symmetric key (HMAC). Si tu fais RSA plus tard, ça changera.
    public required string SigningKey { get; init; }

    // Optionnel: si tu veux centraliser la durée
    public int AccessTokenMinutes { get; init; } = 60;
}