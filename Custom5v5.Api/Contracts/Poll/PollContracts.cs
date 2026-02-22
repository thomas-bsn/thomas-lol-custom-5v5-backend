namespace Custom5v5.Api.Contracts;

public sealed record PlayerDto(Guid Id, string DisplayName);

public enum Grade
{
    A, B, C, D, Blank
}

public sealed record OpenPollRequest(int DurationHours = 24);

public sealed record PollDto(
    Guid PollId,
    DateTime OpensAtUtc,
    DateTime ClosesAtUtc,
    string Status,
    IReadOnlyList<PlayerDto> Players
);

public sealed record SubmitBallotRequest(IReadOnlyList<VoteDto> Votes);

public sealed record VoteDto(Guid PlayerId, Grade Grade);

public sealed record PollResultsDto(Guid PollId, IReadOnlyList<PlayerResultDto> Results);

public sealed record PlayerResultDto(
    Guid PlayerId,
    string DisplayName,
    int A,
    int B,
    int C,
    int D,
    int Blank,
    int Total,
    Grade? Majority
);