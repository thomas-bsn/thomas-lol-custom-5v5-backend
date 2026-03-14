using Custom5v5.Application.DTOs.Poll;

namespace Custom5v5.Api.Contracts;

public sealed record OpenPollRequest(int DurationHours = 24);
public sealed record SubmitBallotRequest(IReadOnlyList<VoteDto> Votes);