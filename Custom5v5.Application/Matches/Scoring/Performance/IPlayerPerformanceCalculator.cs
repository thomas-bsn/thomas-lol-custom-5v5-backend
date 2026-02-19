using System.Collections.Generic;
using Custom5v5.Application.Matches.Dtos;

namespace Custom5v5.Application.Matches.Scoring.Performance
{
    using Custom5v5.Application.Matches.Dtos; // adapte si ton namespace DTO diffère
    using Custom5v5.Application.Matches.Scoring.Performance.Models;

    public interface IPlayerPerformanceCalculator
    {
        PlayerPerformanceResult EvaluatePerformance(MatchDto match, ParticipantDto participant);


        IReadOnlyList<PlayerPerformanceResult> EvaluateAllPlayers(MatchDto match);
    }
}