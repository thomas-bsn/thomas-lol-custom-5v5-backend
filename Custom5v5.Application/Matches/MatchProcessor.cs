using Custom5v5.Application.Matches.Scoring.Performance;

namespace Custom5v5.Application.Matches;

public sealed class MatchProcessor
{
    private readonly IMatchProvider _provider;
    private readonly IPlayerPerformanceCalculator _calculator;

    public MatchProcessor(IMatchProvider provider, IPlayerPerformanceCalculator calculator)
    {
        _provider = provider;
        _calculator = calculator;
    }

    public async Task ProcessAsync(string matchId)
    {
        var match = await _provider.GetFinishedMatchAsync(matchId);

        if (match is null)
        {
            Console.WriteLine("Match pas encore dispo");
            return;
        }

        foreach (var p in match.Info.Participants)
        {
            var PerformanceResult = _calculator.EvaluatePerformance(match, p);
            Console.WriteLine($"{PerformanceResult.SummonerName}: {PerformanceResult.GlobalScore} ({PerformanceResult.GlobalGrade})");   
        }

        Console.WriteLine("Match traité");
    }
}
