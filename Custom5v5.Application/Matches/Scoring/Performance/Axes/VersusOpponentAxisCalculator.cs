// Axes/VersusOpponentAxisCalculator.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Custom5v5.Application.Matches.Dtos;
using Custom5v5.Application.Matches.Scoring.Performance.Models;

namespace Custom5v5.Application.Matches.Scoring.Performance.Axes
{
    public sealed class VersusOpponentAxisCalculator : IPerformanceAxisCalculator
    {
        public string AxisName => "VersusOpponent";

        public AxisScore Evaluate(
            MatchDto match,
            ParticipantDto player,
            IReadOnlyList<ParticipantDto> allParticipants,
            TeamDto playerTeam,
            TeamDto enemyTeam)
        {
            if (match?.Info == null)
                return AxisScore.Failed(AxisName, "match.Info is null");

            var role = NormalizeRole(player.TeamPosition);

            var opp = allParticipants.FirstOrDefault(p =>
                p.TeamId != player.TeamId && NormalizeRole(p.TeamPosition) == role);

            if (opp == null)
            {
                return AxisScore.Create(AxisName, 50, new[]
                {
                    new AxisBreakdownItem { Label = "Opponent", Value = "N/A", Points = 0, Note = "No matching role opponent found" }
                });
            }

            var minutes = Math.Max(1.0, match.Info.GameDuration / 60.0);

            // Raw diffs
            var goldDiff = player.GoldEarned - opp.GoldEarned;

            var csPlayer = player.TotalMinionsKilled + player.NeutralMinionsKilled;
            var csOpp = opp.TotalMinionsKilled + opp.NeutralMinionsKilled;
            var csDiff = csPlayer - csOpp;

            var levelDiff = player.ChampLevel - opp.ChampLevel;

            var visionDiff = player.VisionScore - opp.VisionScore;

            // Normalized diffs per minute (plus stable)
            var goldDiffPerMin = goldDiff / minutes;
            var csDiffPerMin = csDiff / minutes;
            var visionDiffPerMin = visionDiff / minutes;

            // Ranges role-aware (adjustable)
            // idea: min => 0, 0 => 50, max => 100
            var ranges = GetRanges(role);

            var goldScore = DiffToScore(goldDiffPerMin, -ranges.GoldPerMin, ranges.GoldPerMin);
            var csScore = IsSupport(role) ? 50 : DiffToScore(csDiffPerMin, -ranges.CsPerMin, ranges.CsPerMin);
            var lvlScore = DiffToScore(levelDiff, -ranges.Level, ranges.Level);
            var visionScore = DiffToScore(visionDiffPerMin, -ranges.VisionPerMin, ranges.VisionPerMin);

            var (wGold, wCs, wLvl, wVision) = GetWeights(role);

            var score = wGold * goldScore + wCs * csScore + wLvl * lvlScore + wVision * visionScore;
            score = Clamp(score, 0, 100);

            var breakdown = new List<AxisBreakdownItem>
            {
                new AxisBreakdownItem { Label = "Opponent", Value = opp.SummonerName ?? "Unknown", Points = 0 },

                // Show both raw and per-min in Value, points are contribution around neutral (50)
                Item("Gold diff/min",
                    $"{goldDiffPerMin.ToString("0.0", CultureInfo.InvariantCulture)} (raw {goldDiff})",
                    goldScore, wGold,
                    $"range ±{ranges.GoldPerMin:0.0}/min"),

                Item("CS diff/min",
                    $"{csDiffPerMin.ToString("0.00", CultureInfo.InvariantCulture)} (raw {csDiff})",
                    csScore, wCs,
                    IsSupport(role) ? "ignored for supports" : $"range ±{ranges.CsPerMin:0.00}/min"),

                Item("Level diff",
                    levelDiff.ToString(CultureInfo.InvariantCulture),
                    lvlScore, wLvl,
                    $"range ±{ranges.Level}"),

                Item("Vision diff/min",
                    $"{visionDiffPerMin.ToString("0.00", CultureInfo.InvariantCulture)} (raw {visionDiff})",
                    visionScore, wVision,
                    $"range ±{ranges.VisionPerMin:0.00}/min"),

                new AxisBreakdownItem { Label = "Duration", Value = minutes.ToString("0.0", CultureInfo.InvariantCulture) + " min", Points = 0 },
            };

            return AxisScore.Create(AxisName, score, breakdown);
        }

        private static AxisBreakdownItem Item(string label, string value, double subScore0To100, double weight, string note)
        {
            // contribution lisible: (subScore - 50) * weight
            // 50 = neutre, au-dessus = positif, en dessous = négatif
            var points = (subScore0To100 - 50.0) * weight;

            return new AxisBreakdownItem
            {
                Label = label,
                Value = value,
                Points = Math.Round(points, 2),
                Note = $"{note} | subscore {subScore0To100:0.##}/100 | w {weight:0.00}"
            };
        }

        private static (double wGold, double wCs, double wLvl, double wVision) GetWeights(string role)
        {
            // sum = 1
            if (IsSupport(role)) return (0.25, 0.00, 0.15, 0.60);
            if (role == "JUNGLE") return (0.40, 0.10, 0.30, 0.20);
            return (0.45, 0.25, 0.25, 0.05);
        }

        private static (double GoldPerMin, double CsPerMin, int Level, double VisionPerMin) GetRanges(string role)
        {
            // Ces ranges sont volontairement "soft" pour éviter de mettre 0/100 trop vite.
            // Elles doivent être ajustées avec des games réelles.
            if (IsSupport(role))
            {
                return (
                    GoldPerMin: 120,   // ±120 gold/min = énorme pour support
                    CsPerMin: 0.0,     // ignored
                    Level: 2,          // ±2 levels
                    VisionPerMin: 0.80 // ±0.80 vision/min (très gros)
                );
            }

            if (role == "JUNGLE")
            {
                return (
                    GoldPerMin: 180,
                    CsPerMin: 1.8,
                    Level: 2,
                    VisionPerMin: 0.55
                );
            }

            // TOP/MID/BOTTOM
            return (
                GoldPerMin: 220,
                CsPerMin: 2.2,
                Level: 2,
                VisionPerMin: 0.45
            );
        }

        private static double DiffToScore(double diff, double min, double max)
        {
            // map linéaire: min->0, 0->50, max->100
            if (diff <= min) return 0;
            if (diff >= max) return 100;
            var t = (diff - min) / (max - min);
            return 100 * t;
        }

        private static string NormalizeRole(string? teamPosition)
            => string.IsNullOrWhiteSpace(teamPosition) ? "UNKNOWN" : teamPosition.Trim().ToUpperInvariant();

        private static bool IsSupport(string role) => role == "UTILITY" || role == "SUPPORT";

        private static double Clamp(double v, double min, double max) => v < min ? min : (v > max ? max : v);
    }
}
