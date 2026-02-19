using System.Collections.Generic;
using Custom5v5.Application.Matches.Scoring.Performance.Models;

namespace Custom5v5.Application.Matches.Scoring.Performance
{
    public interface IGlobalScoreAggregator
    {
        GlobalScoreResult Aggregate(IReadOnlyCollection<AxisScore> axes);
    }
}