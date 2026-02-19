namespace Custom5v5.Api.Contracts.Matches;

public sealed record ProcessMatchResponse(Guid TrackingId, string MatchId, string Status);