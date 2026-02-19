// AxisScore.cs
using System;
using System.Collections.Generic;

namespace Custom5v5.Application.Matches.Scoring.Performance.Models
{
    public sealed class AxisScore
    {
        public string Axis { get; init; } = string.Empty;

        // 0–100
        public double Score { get; init; }

        public PerformanceGrade Grade { get; init; }

        public IReadOnlyList<AxisBreakdownItem> Breakdown { get; init; }
            = Array.Empty<AxisBreakdownItem>();

        // Pour rendre visible un problème d’axe sans crasher tout le calcul
        public bool IsFailed { get; init; }
        public string? Error { get; init; }

        public static AxisScore Create(string axis, double score0To100, IReadOnlyList<AxisBreakdownItem>? breakdown = null)
        {
            if (string.IsNullOrWhiteSpace(axis))
                throw new ArgumentException("axis must not be null or empty.", nameof(axis));

            var clamped = Clamp01(score0To100);
            return new AxisScore
            {
                Axis = axis.Trim(),
                Score = clamped,
                Grade = PerformanceGradeRules.FromScore(clamped),
                Breakdown = breakdown ?? Array.Empty<AxisBreakdownItem>(),
                IsFailed = false,
                Error = null
            };
        }

        public static AxisScore Failed(string axis, string error, IReadOnlyList<AxisBreakdownItem>? breakdown = null)
        {
            if (string.IsNullOrWhiteSpace(axis))
                axis = "Unknown";

            return new AxisScore
            {
                Axis = axis.Trim(),
                Score = 0,
                Grade = PerformanceGrade.F,
                Breakdown = breakdown ?? Array.Empty<AxisBreakdownItem>(),
                IsFailed = true,
                Error = string.IsNullOrWhiteSpace(error) ? "Axis evaluation failed." : error
            };
        }

        private static double Clamp01(double score0To100)
        {
            if (score0To100 < 0) return 0;
            if (score0To100 > 100) return 100;
            return score0To100;
        }
    }
}
