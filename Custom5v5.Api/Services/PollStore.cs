using System.Collections.Concurrent;
using Custom5v5.Api.Contracts;
using Custom5v5.Api.Interfaces;

namespace Custom5v5.Api.Services;

public sealed class PollStore
{
    private readonly IPlayersSource _playersSource;
    private readonly object _lock = new();

    private PollState? _current;
    private readonly ConcurrentDictionary<string, Ballot> _ballotsByVoter = new();

    public PollStore(IPlayersSource playersSource)
    {
        _playersSource = playersSource;
    }

    public PollDto Open(int durationHours)
    {
        durationHours = Math.Clamp(durationHours, 1, 72);

        lock (_lock)
        {
            var now = DateTime.UtcNow;

            // Snapshot des joueurs au moment de l'ouverture.
            // Important: les votes / résultats se font sur CETTE liste,
            // même si la source change derrière (DB, etc.)
            var playersSnapshot = _playersSource.GetAllSnapshot();

            if (playersSnapshot.Count == 0)
                throw new InvalidOperationException("No players available to vote on.");

            _current = new PollState(
                pollId: Guid.NewGuid(),
                opensAtUtc: now,
                closesAtUtc: now.AddHours(durationHours),
                playersSnapshot: playersSnapshot
            );

            _ballotsByVoter.Clear();

            return ToDto(_current);
        }
    }

    public PollDto? GetCurrent()
    {
        lock (_lock)
        {
            if (_current is null) return null;
            CloseIfExpired_NoThrow(_current);
            return ToDto(_current);
        }
    }

    public void UpsertBallot(string voterId, SubmitBallotRequest req)
    {
        if (string.IsNullOrWhiteSpace(voterId))
            throw new ArgumentException("voterId is required.", nameof(voterId));

        var poll = GetCurrentInternalOrThrow();

        if (poll.Status != PollStatus.Open)
            throw new InvalidOperationException("Poll is closed.");

        // Normalize: one vote per player
        var map = new Dictionary<Guid, Grade>();
        foreach (var v in req.Votes ?? Array.Empty<VoteDto>())
        {
            // Ignore unknown player ids instead of exploding
            if (!poll.PlayersSnapshotById.ContainsKey(v.PlayerId))
                continue;

            map[v.PlayerId] = v.Grade;
        }

        var ballot = new Ballot(map, DateTime.UtcNow);
        _ballotsByVoter[voterId] = ballot;
    }

    public PollResultsDto GetResults()
    {
        var poll = GetCurrentInternalOrThrow();
        var players = poll.PlayersSnapshot;

        // Aggregate per player
        var acc = players.ToDictionary(p => p.Id, p => new Counts(p.DisplayName));

        foreach (var ballot in _ballotsByVoter.Values)
        {
            foreach (var player in players)
            {
                // vote blanc par défaut si pas voté
                var grade = ballot.Votes.TryGetValue(player.Id, out var g) ? g : Grade.Blank;
                acc[player.Id].Add(grade);
            }
        }

        var results = acc.Select(kvp =>
        {
            var playerId = kvp.Key;
            var counts = kvp.Value;
            var total = counts.Total;

            Grade? majority = total == 0 ? null : counts.Majority;

            return new PlayerResultDto(
                playerId,
                counts.DisplayName,
                counts.A,
                counts.B,
                counts.C,
                counts.D,
                counts.Blank,
                total,
                majority
            );
        }).ToList();

        return new PollResultsDto(poll.PollId, results);
    }

    public (bool HasBallot, SubmitBallotRequest? Ballot) TryGetMyBallot(string voterId)
    {
        if (string.IsNullOrWhiteSpace(voterId))
            return (false, null);

        var poll = GetCurrentInternalOrThrow();

        if (!_ballotsByVoter.TryGetValue(voterId, out var ballot))
            return (false, null);

        // Convert stored ballot to request-like DTO for the UI
        var votes = ballot.Votes
            .Select(kv => new VoteDto(kv.Key, kv.Value))
            .ToList();

        return (true, new SubmitBallotRequest(votes));
    }

    private PollState GetCurrentInternalOrThrow()
    {
        lock (_lock)
        {
            if (_current is null)
                throw new InvalidOperationException("No poll open.");

            CloseIfExpired_NoThrow(_current);
            return _current;
        }
    }

    private static void CloseIfExpired_NoThrow(PollState poll)
    {
        if (poll.Status == PollStatus.Closed) return;

        if (DateTime.UtcNow >= poll.ClosesAtUtc)
            poll.Status = PollStatus.Closed;
    }

    private static PollDto ToDto(PollState poll)
        => new(
            poll.PollId,
            poll.OpensAtUtc,
            poll.ClosesAtUtc,
            poll.Status.ToString(),
            poll.PlayersSnapshot
        );

    private sealed class PollState
    {
        public PollState(Guid pollId, DateTime opensAtUtc, DateTime closesAtUtc, IReadOnlyList<PlayerDto> playersSnapshot)
        {
            PollId = pollId;
            OpensAtUtc = opensAtUtc;
            ClosesAtUtc = closesAtUtc;
            PlayersSnapshot = playersSnapshot;

            PlayersSnapshotById = playersSnapshot.ToDictionary(p => p.Id, p => p);
        }

        public Guid PollId { get; }
        public DateTime OpensAtUtc { get; }
        public DateTime ClosesAtUtc { get; }
        public PollStatus Status { get; set; } = PollStatus.Open;

        public IReadOnlyList<PlayerDto> PlayersSnapshot { get; }
        public IReadOnlyDictionary<Guid, PlayerDto> PlayersSnapshotById { get; }
    }

    private enum PollStatus
    {
        Open = 0,
        Closed = 1
    }

    private sealed record Ballot(Dictionary<Guid, Grade> Votes, DateTime UpdatedAtUtc);

    private sealed class Counts
    {
        public Counts(string displayName) => DisplayName = displayName;

        public string DisplayName { get; }
        public int A { get; private set; }
        public int B { get; private set; }
        public int C { get; private set; }
        public int D { get; private set; }
        public int Blank { get; private set; }
        public int Total => A + B + C + D + Blank;

        public void Add(Grade g)
        {
            switch (g)
            {
                case Grade.A: A++; break;
                case Grade.B: B++; break;
                case Grade.C: C++; break;
                case Grade.D: D++; break;
                case Grade.Blank: Blank++; break;
                default: Blank++; break;
            }
        }

        public Grade Majority
        {
            get
            {
                // Majority simple. Tie-break:
                // 1) max count
                // 2) blank last
                // 3) A before B before C before D
                var dict = new Dictionary<Grade, int>
                {
                    { Grade.A, A },
                    { Grade.B, B },
                    { Grade.C, C },
                    { Grade.D, D },
                    { Grade.Blank, Blank }
                };

                return dict
                    .OrderByDescending(kv => kv.Value)
                    .ThenBy(kv => kv.Key == Grade.Blank ? 1 : 0)
                    .ThenBy(kv => kv.Key)
                    .First().Key;
            }
        }
    }
}