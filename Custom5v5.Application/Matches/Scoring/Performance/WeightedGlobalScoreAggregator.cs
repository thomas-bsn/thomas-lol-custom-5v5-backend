using System;
using System.Collections.Generic;
using System.Linq;
using Custom5v5.Application.Matches.Scoring.Performance.Models;

namespace Custom5v5.Application.Matches.Scoring.Performance
{
    public sealed class WeightedGlobalScoreAggregator : IGlobalScoreAggregator
    {
        // Poids par défaut (somme = 1.0)
        private static readonly IReadOnlyDictionary<string, double> DefaultWeights =
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["Global"] = 0.35,
                ["VersusOpponent"] = 0.20,
                ["Objectives"] = 0.15,
                ["TeamImpact"] = 0.15,
                ["RoleImpact"] = 0.15,
            };

        private readonly IReadOnlyDictionary<string, double> _weights;

        public WeightedGlobalScoreAggregator(IReadOnlyDictionary<string, double>? weights = null)
        {
            _weights = weights ?? DefaultWeights;
        }

        public GlobalScoreResult Aggregate(IReadOnlyCollection<AxisScore> axes)
        {
            if (axes is null) throw new ArgumentNullException(nameof(axes));
            if (axes.Count == 0)
                return new GlobalScoreResult { Score = 0, Grade = PerformanceGrade.F };

            double weightedSum = 0;
            double weightSum = 0;

            foreach (var axis in axes)
            {
                var name = axis.Axis ?? string.Empty;
                if (!_weights.TryGetValue(name, out var w)) continue;
                if (w <= 0) continue;

                // Si un axe a failed -> on l’ignore (sinon un bug te ruine tout le score)
                if (axis.IsFailed) continue;

                weightedSum += w * axis.Score;
                weightSum += w;
            }

            // Fallback: si aucun poids matché, moyenne simple des axes valides
            double score;
            if (weightSum <= 0)
            {
                var valid = axes.Where(a => !a.IsFailed).ToList();
                score = valid.Count == 0 ? 0 : valid.Average(a => a.Score);
            }
            else
            {
                score = weightedSum / weightSum;
            }

            score = Clamp(score, 0, 100);
            var rounded = (int)Math.Round(score, MidpointRounding.AwayFromZero);

            return new GlobalScoreResult
            {
                Score = rounded,
                Grade = PerformanceGradeRules.FromScore(rounded)
            };
        }

        private static double Clamp(double v, double min, double max)
            => v < min ? min : (v > max ? max : v);
    }
}
