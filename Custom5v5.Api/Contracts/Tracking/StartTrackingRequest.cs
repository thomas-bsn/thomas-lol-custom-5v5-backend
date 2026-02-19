namespace Custom5v5.Api.Contracts.Tracking;

public sealed class StartTrackingRequest
{
    // JSON attendu: { "tournamentCode": "EUW1-XXXX-XXXX" }
    public string TournamentCode { get; set; } = string.Empty;
}