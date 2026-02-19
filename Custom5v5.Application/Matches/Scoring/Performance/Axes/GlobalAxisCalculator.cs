using System;
using System.Collections.Generic;
using Custom5v5.Application.Matches.Dtos;
using Custom5v5.Application.Matches.Scoring.Performance.Models;

namespace Custom5v5.Application.Matches.Scoring.Performance.Axes
{
    public sealed class GlobalAxisCalculator : IPerformanceAxisCalculator
    {
        public string AxisName => "Global";

        public AxisScore Evaluate(
            MatchDto match,
            ParticipantDto player,
            IReadOnlyList<ParticipantDto> allParticipants,
            TeamDto playerTeam,
            TeamDto enemyTeam)
        {
            if (match?.Info == null) return AxisScore.Failed(AxisName, "match.Info is null");
            if (player == null) return AxisScore.Failed(AxisName, "player is null");
            if (allParticipants == null || allParticipants.Count == 0) return AxisScore.Failed(AxisName, "participants missing");

            var minutes = Math.Max(1.0, match.Info.GameDuration / 60.0);

            var role = NormalizeRole(player.TeamPosition);

            var cs = SafeInt(player.TotalMinionsKilled) + SafeInt(player.NeutralMinionsKilled);
            var csPerMin = cs / minutes;

            var goldPerMin = SafeInt(player.GoldEarned) / minutes;

            var visionPerMin = SafeInt(player.VisionScore) / minutes;

            var deaths = SafeInt(player.Deaths);
            var kills = SafeInt(player.Kills);
            var assists = SafeInt(player.Assists);

            var kda = (kills + assists) / Math.Max(1.0, deaths);

            var exp = RoleExpectations.For(role);

            var csScore = ScoreCsPerMin(csPerMin, exp);
            var goldScore = ScoreGoldPerMin(goldPerMin, exp);
            var visionScore = ScoreVisionPerMin(visionPerMin, exp);
            var deathsScore = ScoreDeaths(deaths, minutes, exp);
            var kdaScore = ScoreKda(kda, exp);

            // combine: 0..100
            var combined =
                0.28 * csScore +
                0.22 * goldScore +
                0.15 * visionScore +
                0.20 * deathsScore +
                0.15 * kdaScore;

            // Floor anti-F absurde
            var floor = ComputePerformanceFloor(role, minutes, csPerMin, deaths, kills, assists, visionPerMin);

            var final = Math.Max(combined, floor);
            final = Clamp(final, 0, 100);

            // Breakdown: Points = contribution au score final (approx via poids)
            var breakdown = new List<AxisBreakdownItem>
            {
                Item("CS/min", csPerMin, csScore, 0.28, $"role {role}, target {exp.CsPerMinTarget:0.0}"),
                Item("Gold/min", goldPerMin, goldScore, 0.22, $"role {role}, target {exp.GoldPerMinTarget:0}"),
                Item("Vision/min", visionPerMin, visionScore, 0.15, $"role {role}, target {exp.VisionPerMinTarget:0.00}"),
                Item("Deaths", deaths, deathsScore, 0.20, $"role {role}, soft cap {exp.DeathsSoftCap:0.0}"),
                Item("KDA", kda, kdaScore, 0.15, $"role {role}, cap {exp.KdaCap:0.0}"),
            };

            if (floor > 0 && floor > combined)
            {
                breakdown.Add(new AxisBreakdownItem
                {
                    Label = "Floor",
                    Value = floor.ToString("0.##"),
                    // Ici Points = combien le floor a "remonté" le score final
                    Points = Math.Round(floor - combined, 2),
                    Note = "anti-F: perf individuelle correcte malgré contexte"
                });
            }

            return new AxisScore
            {
                Axis = AxisName,
                Score = final,
                Grade = PerformanceGradeRules.FromScore(final),
                Breakdown = breakdown
            };
        }

        private static AxisBreakdownItem Item(string label, double rawValue, double metricScore, double weight, string note)
        {
            // metricScore est 0..100, weight est la part dans le global axis
            // contribution (Points) ~ (metricScore - 50) * weight
            // (50 = neutre) => au-dessus tu apportes, en dessous tu enlèves
            var points = (metricScore - 50.0) * weight;

            return new AxisBreakdownItem
            {
                Label = label,
                Value = rawValue.ToString("0.##"),
                Points = Math.Round(points, 2),
                Note = $"{note} | subscore {metricScore:0.##}/100"
            };
        }

        // ---------------------------
        // Sub-scores 0..100
        // ---------------------------

        private static double ScoreCsPerMin(double csPerMin, RoleExpectations exp)
        {
            if (exp.Role == "UTILITY")
                return ScoreLinearClamped(csPerMin, 0.2, 2.0);

            var t = exp.CsPerMinTarget;
            return ScorePiecewise(csPerMin, low: 0.60 * t, mid: 1.00 * t, high: 1.20 * t);
        }

        private static double ScoreGoldPerMin(double gpm, RoleExpectations exp)
        {
            var t = exp.GoldPerMinTarget;
            return ScorePiecewise(gpm, low: 0.70 * t, mid: 1.00 * t, high: 1.20 * t);
        }

        private static double ScoreVisionPerMin(double vpm, RoleExpectations exp)
        {
            var t = exp.VisionPerMinTarget;

            if (exp.Role == "UTILITY")
                return ScorePiecewise(vpm, low: 0.70 * t, mid: 1.00 * t, high: 1.30 * t);

            return ScorePiecewise(vpm, low: 0.50 * t, mid: 1.00 * t, high: 1.50 * t);
        }

        private static double ScoreDeaths(int deaths, double minutes, RoleExpectations exp)
        {
            var dpm = deaths / Math.Max(1.0, minutes);

            var softDeaths = exp.DeathsSoftCap;
            var softDpm = softDeaths / Math.Max(1.0, minutes);

            if (dpm <= 0) return 100;

            var ratio = dpm / Math.Max(0.0001, softDpm);

            var score =
                ratio <= 0.5 ? 90 + 10 * (0.5 - ratio) / 0.5 :
                ratio <= 1.0 ? 90 - 30 * (ratio - 0.5) / 0.5 :
                ratio <= 1.5 ? 60 - 25 * (ratio - 1.0) / 0.5 :
                ratio <= 2.0 ? 35 - 20 * (ratio - 1.5) / 0.5 :
                15;

            return Clamp(score, 10, 100);
        }

        private static double ScoreKda(double kda, RoleExpectations exp)
        {
            var capped = Math.Min(kda, exp.KdaCap);
            return ScorePiecewise(capped, low: 1.0, mid: 2.0, high: 3.5);
        }

        private static double ComputePerformanceFloor(
            string role,
            double minutes,
            double csPerMin,
            int deaths,
            int kills,
            int assists,
            double visionPerMin)
        {
            var kp = kills + assists;

            var lowDeaths = deaths <= (role == "UTILITY" ? 6 : 5);
            var active = kp >= (role == "UTILITY" ? 16 : 12);

            if (!lowDeaths || !active) return 0;

            if (role == "UTILITY")
            {
                if (visionPerMin >= 1.3) return 62;
                if (visionPerMin >= 1.0) return 58;
                return 54;
            }

            if (role == "JUNGLE")
            {
                return csPerMin >= 3.0 ? 60 : 56;
            }

            var minCs = role == "BOTTOM" ? 6.5 : 6.0;
            if (csPerMin >= minCs) return 65;
            if (csPerMin >= minCs - 0.7) return 60;
            return 55;
        }

        // ---------------------------
        // Helpers
        // ---------------------------

        private static int SafeInt(int v) => v < 0 ? 0 : v;

        private static string NormalizeRole(string? teamPosition)
        {
            var r = (teamPosition ?? "").Trim().ToUpperInvariant();
            return r switch
            {
                "TOP" => "TOP",
                "JUNGLE" => "JUNGLE",
                "MIDDLE" => "MIDDLE",
                "BOTTOM" => "BOTTOM",
                "UTILITY" => "UTILITY",
                _ => "UNKNOWN"
            };
        }

        private static double ScoreLinearClamped(double value, double min, double max)
        {
            if (max <= min) return 50;
            var t = (value - min) / (max - min);
            return Clamp(100 * t, 0, 100);
        }

        private static double ScorePiecewise(double value, double low, double mid, double high)
        {
            if (high <= mid) high = mid * 1.2;
            if (mid <= low) mid = low * 1.2;

            if (value <= low) return ScoreLinearClamped(value, 0, low) * 0.5;
            if (value <= mid) return 50 + 30 * (value - low) / (mid - low);
            if (value <= high) return 80 + 20 * (value - mid) / (high - mid);
            return 100;
        }

        private static double Clamp(double v, double min, double max)
            => v < min ? min : (v > max ? max : v);

        private sealed class RoleExpectations
        {
            public string Role { get; init; } = "UNKNOWN";
            public double CsPerMinTarget { get; init; }
            public double GoldPerMinTarget { get; init; }
            public double VisionPerMinTarget { get; init; }
            public double DeathsSoftCap { get; init; }
            public double KdaCap { get; init; }

            public static RoleExpectations For(string role)
            {
                return role switch
                {
                    "TOP" => new RoleExpectations
                    {
                        Role = "TOP",
                        CsPerMinTarget = 6.5,
                        GoldPerMinTarget = 420,
                        VisionPerMinTarget = 0.55,
                        DeathsSoftCap = 6,
                        KdaCap = 6
                    },
                    "JUNGLE" => new RoleExpectations
                    {
                        Role = "JUNGLE",
                        CsPerMinTarget = 5.5,
                        GoldPerMinTarget = 410,
                        VisionPerMinTarget = 0.65,
                        DeathsSoftCap = 6,
                        KdaCap = 6
                    },
                    "MIDDLE" => new RoleExpectations
                    {
                        Role = "MIDDLE",
                        CsPerMinTarget = 7.0,
                        GoldPerMinTarget = 450,
                        VisionPerMinTarget = 0.55,
                        DeathsSoftCap = 6,
                        KdaCap = 6
                    },
                    "BOTTOM" => new RoleExpectations
                    {
                        Role = "BOTTOM",
                        CsPerMinTarget = 7.5,
                        GoldPerMinTarget = 460,
                        VisionPerMinTarget = 0.50,
                        DeathsSoftCap = 6.5,
                        KdaCap = 7
                    },
                    "UTILITY" => new RoleExpectations
                    {
                        Role = "UTILITY",
                        CsPerMinTarget = 1.2,
                        GoldPerMinTarget = 320,
                        VisionPerMinTarget = 1.35,
                        DeathsSoftCap = 6.0,
                        KdaCap = 5
                    },
                    _ => new RoleExpectations
                    {
                        Role = "UNKNOWN",
                        CsPerMinTarget = 6.5,
                        GoldPerMinTarget = 420,
                        VisionPerMinTarget = 0.55,
                        DeathsSoftCap = 6,
                        KdaCap = 6
                    }
                };
            }
        }
    }
}
