// Axes/TeamImpactAxisCalculator.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Custom5v5.Application.Matches.Dtos;
using Custom5v5.Application.Matches.Scoring.Performance.Models;

namespace Custom5v5.Application.Matches.Scoring.Performance.Axes
{
    public sealed class TeamImpactAxisCalculator : IPerformanceAxisCalculator
    {
        public string AxisName => "TeamImpact";

        public AxisScore Evaluate(
            MatchDto match,
            ParticipantDto player,
            IReadOnlyList<ParticipantDto> allParticipants,
            TeamDto playerTeam,
            TeamDto enemyTeam)
        {
            var role = NormalizeRole(player.TeamPosition);

            var teamKills = playerTeam.Objectives?.Champion?.Kills ?? 0;
            var playerKA = player.Kills + player.Assists;
            var kp = teamKills <= 0 ? 0 : (playerKA / (double)Math.Max(1, teamKills)); // 0..1+

            // KP score: 0% -> 0, 50% -> 70, 70% -> 90, 90%+ -> 100
            var kpScore = PercentToScore(kp);

            // “Cleanliness” proxy: deaths per min (role-aware)
            var minutes = Math.Max(0.1, (match.Info?.GameDuration ?? 1) / 60.0);
            var deathsPerMin = player.Deaths / minutes;
            var deathsScore = DeathsPerMinToScore(role, deathsPerMin);

            var (wKp, wDeaths) = GetWeights(role);
            var score = Clamp(wKp * kpScore + wDeaths * deathsScore, 0, 100);

            var breakdown = new List<AxisBreakdownItem>
            {
                new AxisBreakdownItem { Label = "Kill participation", Value = (kp * 100).ToString("0.0", CultureInfo.InvariantCulture) + "%", Points = Points(wKp, kpScore) },
                new AxisBreakdownItem { Label = "Team kills", Value = teamKills.ToString(CultureInfo.InvariantCulture), Points = 0 },
                new AxisBreakdownItem { Label = "Deaths/min", Value = deathsPerMin.ToString("0.00", CultureInfo.InvariantCulture), Points = Points(wDeaths, deathsScore), Note = IsSupport(role) ? "Support deaths punished harder" : null },
            };

            return AxisScore.Create(AxisName, score, breakdown);
        }

        private static (double wKp, double wDeaths) GetWeights(string role)
        {
            if (IsSupport(role)) return (0.65, 0.35);
            if (role == "JUNGLE") return (0.70, 0.30);
            return (0.75, 0.25);
        }

        private static double PercentToScore(double kp)
        {
            if (kp <= 0) return 0;
            if (kp >= 0.9) return 100;
            if (kp >= 0.7) return 90 + (kp - 0.7) * (10 / 0.2);
            if (kp >= 0.5) return 70 + (kp - 0.5) * (20 / 0.2);
            // 0..50% => 0..70
            return (kp / 0.5) * 70;
        }

        private static double DeathsPerMinToScore(string role, double dpm)
        {
            // bornes “raisonnables”:
            // support: 0.15 très bien, 0.30 ok, 0.45 mauvais, 0.60 horrible
            // carry:   0.12 très bien, 0.25 ok, 0.40 mauvais, 0.55 horrible
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
