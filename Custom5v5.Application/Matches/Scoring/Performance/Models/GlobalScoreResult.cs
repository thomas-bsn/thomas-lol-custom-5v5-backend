namespace Custom5v5.Application.Matches.Scoring.Performance.Models
{
    public sealed class GlobalScoreResult
    {
        public int Score { get; init; } // 0–100
        public PerformanceGrade Grade { get; init; }
    }
}