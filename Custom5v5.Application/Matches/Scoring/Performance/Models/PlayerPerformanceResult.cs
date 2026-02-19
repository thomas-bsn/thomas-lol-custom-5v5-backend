using System.Collections.Generic;

namespace Custom5v5.Application.Matches.Scoring.Performance.Models
{
    public sealed class PlayerPerformanceResult
    {
        // Identité (utile pour API / UI / debug)
        public string? Puuid { get; init; }
        public string? SummonerName { get; init; }
        public int TeamId { get; init; }

        // Résultat global
        public double GlobalScore { get; init; }          // 0–100 (double côté domaine)
        public PerformanceGrade GlobalGrade { get; init; }

        // Détail par axe
        public IReadOnlyDictionary<string, AxisScore> Axes { get; init; }
            = new Dictionary<string, AxisScore>();
    }
}