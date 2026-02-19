namespace Custom5v5.Api.Contracts.Tracking;

public sealed class StartTrackingResponse
{
    public Guid TrackingId { get; set; }
    public string TournamentCode { get; set; } = string.Empty;
}