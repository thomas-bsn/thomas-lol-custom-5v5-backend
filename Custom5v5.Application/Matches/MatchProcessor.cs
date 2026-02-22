namespace Custom5v5.Application.Matches;

public sealed class MatchProcessor
{
    private readonly IMatchProvider _provider;

    public MatchProcessor(IMatchProvider provider)
    {
        _provider = provider;
    }

    public async Task ProcessAsync(string matchId)
    {
        var match = await _provider.GetFinishedMatchAsync(matchId);

        if (match is null)
        {
            Console.WriteLine("Match pas encore dispo");
            return;
        }

        Console.WriteLine("Match traité");
    }
}
