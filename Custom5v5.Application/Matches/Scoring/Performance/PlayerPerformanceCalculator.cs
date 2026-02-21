using System;
using System.Collections.Generic;
using System.Linq;
using Custom5v5.Application.Matches.Dtos;

namespace Custom5v5.Application.Matches.Scoring.Performance
{
    using Custom5v5.Application.Matches.Scoring.Performance.Axes;
    using Custom5v5.Application.Matches.Scoring.Performance.Models;

    public sealed class PlayerPerformanceCalculator : IPlayerPerformanceCalculator
    {
        private readonly IReadOnlyList<IPerformanceAxisCalculator> _axes;
        private readonly IGlobalScoreAggregator _aggregator;

        public PlayerPerformanceCalculator(
            IEnumerable<IPerformanceAxisCalculator> axes,
            IGlobalScoreAggregator aggregator)
        {
            if (axes is null) throw new ArgumentNullException(nameof(axes));
            _aggregator = aggregator ?? throw new ArgumentNullException(nameof(aggregator));

            var list = axes.Where(a => a != null).ToList();
            if (list.Count == 0)
                throw new ArgumentException("At least one axis calculator must be provided.", nameof(axes));

            var emptyName = list.FirstOrDefault(a => string.IsNullOrWhiteSpace(a.AxisName));
            if (emptyName != null)
                throw new ArgumentException("AxisName must not be null or empty.", nameof(axes));

            var dup = list
                .GroupBy(a => a.AxisName.Trim(), StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(g => g.Count() > 1);

            if (dup != null)
                throw new ArgumentException(
                    $"Duplicate AxisName detected: '{dup.Key}'. Axis names must be unique.",
                    nameof(axes));

            _axes = list;
        }

        // Point d’entrée recommandé pour ton contexte tournoi privé
        public PlayerPerformanceResult EvaluatePerformance(MatchDto match, ParticipantDto player)
        {
            if (match is null) throw new ArgumentNullException(nameof(match));
            if (player is null) throw new ArgumentNullException(nameof(player));

            var (participants, teams) = GetParticipantsAndTeams(match);

            if (!participants.Contains(player))
                throw new ArgumentException("player must belong to match.Info.Participants.", nameof(player));

            return EvaluateInternal(match, player, participants, teams);
        }

        public IReadOnlyList<PlayerPerformanceResult> EvaluateAllPlayers(MatchDto match)
        {
            if (match is null) throw new ArgumentNullException(nameof(match));

            var (participants, teams) = GetParticipantsAndTeams(match);

            var results = new List<PlayerPerformanceResult>(participants.Count);
            foreach (var p in participants)
            {
                results.Add(EvaluateInternal(match, p, participants, teams));
            }

            return results;
        }

        private PlayerPerformanceResult EvaluateInternal(
            MatchDto match,
            ParticipantDto player,
            IReadOnlyList<ParticipantDto> participants,
            IReadOnlyList<TeamDto> teams)
        {
            var playerTeam = teams.FirstOrDefault(t => t.TeamId == player.TeamId)
                ?? throw new InvalidOperationException($"Team '{player.TeamId}' not found.");

            var enemyTeam = teams.FirstOrDefault(t => t.TeamId != player.TeamId)
                ?? throw new InvalidOperationException("Enemy team not found (expected exactly 2 teams).");

            var axisScores = new Dictionary<string, AxisScore>(StringComparer.OrdinalIgnoreCase);

            foreach (var axis in _axes)
            {
                AxisScore score;
                try
                {
                    score = axis.Evaluate(match, player, participants, playerTeam, enemyTeam);
                }
                catch (Exception ex)
                {
                    // visible, explicable, pas silencieux
                    score = AxisScore.Failed(axis.AxisName, ex.Message);
                }

                axisScores[score.Axis ?? axis.AxisName] = score;
            }

            var global = _aggregator.Aggregate(axisScores.Values);

            return new PlayerPerformanceResult
            {
                Puuid = player.Puuid,
                SummonerName = player.RiotIdGameName,
                TeamId = player.TeamId,
                GlobalScore = global.Score,
                GlobalGrade = global.Grade,
                Axes = axisScores
            };
        }

        private static (IReadOnlyList<ParticipantDto>, IReadOnlyList<TeamDto>) GetParticipantsAndTeams(MatchDto match)
        {
            var participants = match.Info?.Participants?.ToList()
                ?? throw new ArgumentException("match.Info.Participants is required.", nameof(match));

            var teams = match.Info?.Teams?.ToList()
                ?? throw new ArgumentException("match.Info.Teams is required.", nameof(match));

            if (participants.Count == 0)
                throw new ArgumentException("match.Info.Participants is empty.", nameof(match));

            if (teams.Count < 2)
                throw new ArgumentException("match.Info.Teams must contain at least 2 teams.", nameof(match));

            return (participants, teams);
        }
    }
}
