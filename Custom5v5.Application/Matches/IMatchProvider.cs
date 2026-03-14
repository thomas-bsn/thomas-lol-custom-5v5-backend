using Custom5v5.Application.Matches.Dtos;

namespace Custom5v5.Application.Matches;

public interface IMatchProvider
{
    Task<MatchDto?> GetFinishedMatchAsync(string matchId);
}
