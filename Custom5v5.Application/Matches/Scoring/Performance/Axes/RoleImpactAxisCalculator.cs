// Axes/RoleImpactAxisCalculator.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Custom5v5.Application.Matches.Dtos;
using Custom5v5.Application.Matches.Scoring.Performance.Models;

namespace Custom5v5.Application.Matches.Scoring.Performance.Axes
{
    public sealed class RoleImpactAxisCalculator : IPerformanceAxisCalculator
    {
        public string AxisName => "RoleImpact";

        public AxisScore Evaluate(
            MatchDto match,
            ParticipantDto player,
            IReadOnlyList<ParticipantDto> allParticipants,
            TeamDto playerTeam,
            TeamDto enemyTeam)
        {
            var minutes = Math.Max(0.1, (match.Info?.GameDuration ?? 1) / 60.0);
            var role = NormalizeRole(player.TeamPosition);

            var rolePeers = allParticipants.Where(p => NormalizeRole(p.TeamPosition) == role).ToList();
            if (rolePeers.Count == 0) rolePeers = allParticipants.ToList();

            // Base metrics
            var deathsPerMin = player.Deaths / minutes;
            var deathsScore = DeathsPerMinToScore(role, deathsPerMin);

            var visionScore = RankToScore(rolePeers, p => p.VisionScore, true, player);

            var cs = player.TotalMinionsKilled + player.NeutralMinionsKilled;
            var csPerMin = cs / minutes;
            var csScore = IsSupport(role) ? 50 : RankToScore(rolePeers, p =>
            {
                var c = p.TotalMinionsKilled + p.NeutralMinionsKilled;
                return c / minutes;
            }, true, player);

            var goldPerMin = player.GoldEarned / minutes;
            var goldScore = RankToScore(rolePeers, p => p.GoldEarned / minutes, true, player);

            var wardsPlacedScore = IsSupport(role) ? RankToScore(rolePeers, p => p.WardsPlaced, true, player) : 50;
            var wardsKilledScore = IsSupport(role) ? RankToScore(rolePeers, p => p.WardsKilled, true, player) : 50;
            var controlWardsScore = IsSupport(role) ? RankToScore(rolePeers, p => p.VisionWardsBoughtInGame, true, player) : 50;

            var jungleFarmScore = role == "JUNGLE" ? RankToScore(rolePeers, p => p.NeutralMinionsKilled, true, player) : 50;

            var (wDeaths, wVision, wCs, wGold, wWardsPlaced, wWardsKilled, wControlWards, wJungleFarm) = GetWeights(role);

            var score =
                wDeaths * deathsScore +
                wVision * visionScore +
                wCs * csScore +
                wGold * goldScore +
                wWardsPlaced * wardsPlacedScore +
                wWardsKilled * wardsKilledScore +
                wControlWards * controlWardsScore +
                wJungleFarm * jungleFarmScore;

            score = Clamp(score, 0, 100);

            var breakdown = new List<AxisBreakdownItem>
            {
                new AxisBreakdownItem { Label = "Role", Value = role, Points = 0 },
                new AxisBreakdownItem { Label = "Deaths/min", Value = deathsPerMin.ToString("0.00", CultureInfo.InvariantCulture), Points = Points(wDeaths, deathsScore), Note = IsSupport(role) ? "Support deaths punished harder" : null },
                new AxisBreakdownItem { Label = "Vision", Value = player.VisionScore.ToString(CultureInfo.InvariantCulture), Points = Points(wVision, visionScore) },
                new AxisBreakdownItem { Label = "Gold/min", Value = goldPerMin.ToString("0.0", CultureInfo.InvariantCulture), Points = Points(wGold, goldScore) },
                new AxisBreakdownItem { Label = "CS/min", Value = csPerMin.ToString("0.0", CultureInfo.InvariantCulture), Points = IsSupport(role) ? 0 : Points(wCs, csScore), Note = IsSupport(role) ? "CS ignored for supports" : null },
            };

            if (IsSupport(role))
            {
                breakdown.Add(new AxisBreakdownItem { Label = "Wards placed", Value = player.WardsPlaced.ToString(CultureInfo.InvariantCulture), Points = Points(wWardsPlaced, wardsPlacedScore) });
                breakdown.Add(new AxisBreakdownItem { Label = "Wards killed", Value = player.WardsKilled.ToString(CultureInfo.InvariantCulture), Points = Points(wWardsKilled, wardsKilledScore) });
                breakdown.Add(new AxisBreakdownItem { Label = "Control wards", Value = player.VisionWardsBoughtInGame.ToString(CultureInfo.InvariantCulture), Points = Points(wControlWards, controlWardsScore) });
            }

            if (role == "JUNGLE")
            {
                breakdown.Add(new AxisBreakdownItem { Label = "Neutral CS", Value = player.NeutralMinionsKilled.ToString(CultureInfo.InvariantCulture), Points = Points(wJungleFarm, jungleFarmScore) });
            }

            return AxisScore.Create(AxisName, score, breakdown);
        }

        private static (double wDeaths, double wVision, double wCs, double wGold, double wWardsPlaced, double wWardsKilled, double wControlWards, double wJungleFarm)
            GetWeights(string role)
        {
            // Somme = 1.0, volontairement simple
            if (IsSupport(role)) return (0.40, 0.25, 0.00, 0.05, 0.10, 0.10, 0.10, 0.00);
            if (role == "JUNGLE") return (0.25, 0.20, 0.10, 0.15, 0.00, 0.00, 0.00, 0.30);
            return (0.25, 0.05, 0.35, 0.35, 0.00, 0.00, 0.00, 0.00);
        }

        private static double RankToScore(
            IReadOnlyList<ParticipantDto> peers,
            Func<ParticipantDto, double> valueSelector,
            bool higherIsBetter,
            ParticipantDto player)
        {
            if (peers.Count <= 1) return 50;

            var ordered = higherIsBetter
                ? peers.OrderByDescending(valueSelector).ToList()
                : peers.OrderBy(valueSelector).ToList();

            var idx = ordered.FindIndex(p => p.ParticipantId == player.ParticipantId);
            if (idx < 0) idx = ordered.FindIndex(p => !string.IsNullOrWhiteSpace(p.Puuid) &&
                                                     string.Equals(p.Puuid, player.Puuid, StringComparison.OrdinalIgnoreCase));
            if (idx < 0) return 50;

            var denom = Math.Max(1, ordered.Count - 1);
            return 100.0 * (1.0 - (idx / (double)denom));
        }

        private static double DeathsPerMinToScore(string role, double dpm)
        {
            var (good, ok, bad, awful) = IsSupport(role)
                ? (0.15, 0.30, 0.45, 0.60)
                : (0.12, 0.25, 0.40, 0.55);

            if (dpm <= good) return 100;
            if (dpm <= ok) return Lerp(100, 75, (dpm - good) / (ok - good));
            if (dpm <= bad) return Lerp(75, 40, (dpm - ok) / (bad - ok));
            if (dpm <= awful) return Lerp(40, 0, (dpm - bad) / (awful - bad));
            return 0;
        }

        private static double Lerp(double a, double b, double t) => a + (b - a) * Clamp(t, 0, 1);

        private static string NormalizeRole(string? teamPosition)
            => string.IsNullOrWhiteSpace(teamPosition) ? "UNKNOWN" : teamPosition.Trim().ToUpperInvariant();

        private static bool IsSupport(string role) => role == "UTILITY" || role == "SUPPORT";
        private static double Clamp(double v, double min, double max) => v < min ? min : (v > max ? max : v);
        private static double Points(double weight, double subScore0To100) => Math.Round((weight * subScore0To100) / 10.0, 1);
    }
}
