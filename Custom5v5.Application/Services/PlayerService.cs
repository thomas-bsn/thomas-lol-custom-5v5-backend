using Custom5v5.Application.DTOs.Players;
using Custom5v5.Application.Interfaces;

namespace Custom5v5.Api.Services;

public class PlayerService : IPlayerService
{
    private readonly IPlayerRepository _players;
    private readonly IRiotService _riot;

    public PlayerService(IPlayerRepository players, IRiotService riot)
    {
        _players = players;
        _riot = riot;
    }

    public Task<List<PlayerDto>> GetPlayersAsync() =>
        _players.GetAllAsync();

    public async Task<PlayerResponse> CreatePlayerAsync(string prenom, string riotId)
    {
        if (string.IsNullOrWhiteSpace(prenom) || string.IsNullOrWhiteSpace(riotId))
            throw new ArgumentException("Invalid player data");

        if (await _players.ExistsByRiotIdAsync(riotId))
            throw new InvalidOperationException("Player already exists");

        var puuid = await _riot.GetPuuidFromRiotIdAsync(riotId)
                    ?? throw new InvalidOperationException("Riot account not found");

        var rank = await _riot.GetRankFromPuuidAsync(puuid);

        var player = new PlayerDto
        {
            Prenom = prenom,
            RiotId = riotId,
            PUUID = puuid,
            RankTier = rank?.Tier,
            RankDivision = rank != null ? ParseDivision(rank.Division) : null,
        };

        var created = await _players.AddAsync(player);

        return new PlayerResponse
        {
            Id = created.Id,
            Prenom = created.Prenom,
            RiotId = created.RiotId
        };
    }

    private static int ParseDivision(string division) => division switch
    {
        "I" => 1, "II" => 2, "III" => 3, "IV" => 4, _ => 0
    };
}