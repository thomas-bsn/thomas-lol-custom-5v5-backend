// Axes/ObjectivesAxisCalculator.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using Custom5v5.Application.Matches.Dtos;
using Custom5v5.Application.Matches.Scoring.Performance.Models;

namespace Custom5v5.Application.Matches.Scoring.Performance.Axes
{
    public sealed class ObjectivesAxisCalculator : IPerformanceAxisCalculator
    {
        public string AxisName => "Objectives";

        public AxisScore Evaluate(
            MatchDto match,
            ParticipantDto player,
            IReadOnlyList<ParticipantDto> allParticipants,
            TeamDto playerTeam,
            TeamDto enemyTeam)
        {
            if (match?.Info == null) return AxisScore.Failed(AxisName, "match.Info is null");

            var role = NormalizeRole(player.TeamPosition);
            var minutes = Math.Max(1.0, match.Info.GameDuration / 60.0);

            // Team objective context (BONUS ONLY, capped)
            var ctx = ComputeObjectiveContext(playerTeam, enemyTeam); // -1..+1 approx
            var ctxScore = Clamp(50 + 20 * ctx, 40, 60); // stays near neutral

            // Individual proxies (per minute)
            var vpm = (player.VisionScore) / minutes;
            var controlWards = player.VisionWardsBoughtInGame;
            var wardsKilled = player.WardsKilled;

            var cwpm = controlWards / minutes;
            var wkpm = wardsKilled / minutes;

            var neutralCs = player.NeutralMinionsKilled;
            var ncsPerMin = neutralCs / minutes;

            // Subscores 0..100 (ABSOLUTE, not rank-based)
            var vpmScore = ScorePiecewise(vpm, low: 0.4, mid: RoleVisionMid(role), high: RoleVisionHigh(role));
            var cwpmScore = ScorePiecewise(cwpm, low: 0.03, mid: RoleControlMid(role), high: RoleControlHigh(role));
            var wkpmScore = ScorePiecewise(wkpm, low: 0.03, mid: RoleClearMid(role), high: RoleClearHigh(role));

            var presenceScore = role == "JUNGLE"
                ? ScorePiecewise(ncsPerMin, low: 1.8, mid: 2.6, high: 3.4)
                : 50;

            // Weights: context is SMALL everywhere
            var (wVision, wControl, wClear, wPresence, wContext) = GetWeights(role);

            var score =
                wVision * vpmScore +
                wControl * cwpmScore +
                wClear * wkpmScore +
                wPresence * presenceScore +
                wContext * ctxScore;

            score = Clamp(score, 0, 100);

            // Breakdown points around neutral 50 (explainable)
            var breakdown = new List<AxisBreakdownItem>
            {
                new AxisBreakdownItem { Label = "Vision/min", Value = vpm.ToString("0.00", CultureInfo.InvariantCulture), Points = Points(wVision, vpmScore), Note = $"sub {vpmScore:0.##}/100" },
                new AxisBreakdownItem { Label = "Ctrl wards/min", Value = cwpm.ToString("0.00", CultureInfo.InvariantCulture), Points = Points(wControl, cwpmScore), Note = $"sub {cwpmScore:0.##}/100" },
                new AxisBreakdownItem { Label = "Wards killed/min", Value = wkpm.ToString("0.00", CultureInfo.InvariantCulture), Points = Points(wClear, wkpmScore), Note = $"sub {wkpmScore:0.##}/100" },
            };

            if (role == "JUNGLE")
            {
                breakdown.Add(new AxisBreakdownItem
                {
                    Label = "Neutral CS/min",
                    Value = ncsPerMin.ToString("0.00", CultureInfo.InvariantCulture),
                    Points = Points(wPresence, presenceScore),
                    Note = $"sub {presenceScore:0.##}/100 (proxy presence)"
                });
            }

            breakdown.Add(new AxisBreakdownItem
            {
                Label = "Objective context",
                Value = ctxScore.ToString("0.0", CultureInfo.InvariantCulture),
                Points = Points(wContext, ctxScore),
                Note = "small capped bonus; not win/lose punishment"
            });

            // Also show raw team objectives (informational)
            var teamObj = playerTeam.Objectives;
            breakdown.Add(new AxisBreakdownItem { Label = "Dragons (team)", Value = (teamObj?.Dragon?.Kills ?? 0).ToString(CultureInfo.InvariantCulture), Points = 0 });
            breakdown.Add(new AxisBreakdownItem { Label = "Barons (team)", Value = (teamObj?.Baron?.Kills ?? 0).ToString(CultureInfo.InvariantCulture), Points = 0 });
            breakdown.Add(new AxisBreakdownItem { Label = "Herald (team)", Value = (teamObj?.RiftHerald?.Kills ?? 0).ToString(CultureInfo.InvariantCulture), Points = 0 });
            breakdown.Add(new AxisBreakdownItem { Label = "Towers (team)", Value = (teamObj?.Tower?.Kills ?? 0).ToString(CultureInfo.InvariantCulture), Points = 0 });

            return AxisScore.Create(AxisName, score, breakdown);
        }

        private static (double wVision, double wControl, double wClear, double wPresence, double wContext) GetWeights(string role)
        {
            // sum = 1. context is always small
            if (role == "JUNGLE") return (0.30, 0.10, 0.15, 0.35, 0.10);
            if (IsSupport(role)) return (0.45, 0.20, 0.20, 0.00, 0.15);
            // lanes: mostly their own proxies, not team result
            return (0.35, 0.15, 0.20, 0.00, 0.30);
        }

        private static double ComputeObjectiveContext(TeamDto playerTeam, TeamDto enemyTeam)
        {
            // returns approx in [-1..+1], lightweight
            var a = playerTeam.Objectives;
            var b = enemyTeam.Objectives;

            var d = (a?.Dragon?.Kills ?? 0) - (b?.Dragon?.Kills ?? 0);
            var bar = (a?.Baron?.Kills ?? 0) - (b?.Baron?.Kills ?? 0);
            var h = (a?.RiftHerald?.Kills ?? 0) - (b?.RiftHerald?.Kills ?? 0);
            var t = (a?.Tower?.Kills ?? 0) - (b?.Tower?.Kills ?? 0);

            // weights: baron matters more, but we normalize down
            var raw = d * 0.25 + bar * 0.60 + h * 0.20 + t * 0.05;
            return Clamp(raw, -1.0, 1.0);
        }

        private static double RoleVisionMid(string role)
            => IsSupport(role) ? 1.10 : role == "JUNGLE" ? 0.75 : 0.55;

        private static double RoleVisionHigh(string role)
            => IsSupport(role) ? 1.50 : role == "JUNGLE" ? 1.00 : 0.75;

        private static double RoleControlMid(string role)
            => IsSupport(role) ? 0.20 : role == "JUNGLE" ? 0.10 : 0.07;

        private static double RoleControlHigh(string role)
            => IsSupport(role) ? 0.30 : role == "JUNGLE" ? 0.16 : 0.11;

        private static double RoleClearMid(string role)
            => IsSupport(role) ? 0.20 : role == "JUNGLE" ? 0.17 : 0.10;

        private static double RoleClearHigh(string role)
            => IsSupport(role) ? 0.30 : role == "JUNGLE" ? 0.25 : 0.16;

        private static double ScorePiecewise(double value, double low, double mid, double high)
        {
            if (high <= mid) high = mid * 1.2;
            if (mid <= low) mid = low * 1.2;

            if (value <= low) return ScoreLinearClamped(value, 0, low) * 0.5; // 0..50
            if (value <= mid) return 50 + 30 * (value - low) / (mid - low);  // 50..80
            if (value <= high) return 80 + 20 * (value - mid) / (high - mid); // 80..100
            return 100;
        }

        private static double ScoreLinearClamped(double value, double min, double max)
        {
            if (max <= min) return 50;
            var t = (value - min) / (max - min);
            return Clamp(100 * t, 0, 100);
        }

        private static double Points(double weight, double subScore0To100)
        {
            // contribution around neutral 50
            return Math.Round((subScore0To100 - 50.0) * weight, 2);
        }

        private static string NormalizeRole(string? teamPosition)
            => string.IsNullOrWhiteSpace(teamPosition) ? "UNKNOWN" : teamPosition.Trim().ToUpperInvariant();

        private static bool IsSupport(string role) => role == "UTILITY" || role == "SUPPORT";
        private static double Clamp(double v, double min, double max) => v < min ? min : (v > max ? max : v);
    }
}
