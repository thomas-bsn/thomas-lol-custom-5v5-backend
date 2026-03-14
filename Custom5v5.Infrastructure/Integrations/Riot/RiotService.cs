using System.Net.Http.Json;
using Custom5v5.Application.DTOs.Riot;
using Custom5v5.Application.Interfaces;

public class RiotService : IRiotService
{
    private readonly HttpClient _http;

    public RiotService(HttpClient http) => _http = http;

    // Point d'entrée principal : riotId → rank
    public async Task<RiotRankDto?> GetRankAsync(string riotId)
    {
        var puuid = await GetPuuidFromRiotIdAsync(riotId);
        if (puuid == null) return null;

        return await GetRankFromPuuidAsync(puuid);
    }

    // Exposé publiquement : riotId → puuid
    public async Task<string?> GetPuuidFromRiotIdAsync(string riotId)
    {
        var (gameName, tagLine) = SplitRiotId(riotId);
        return await GetPuuidAsync(gameName, tagLine);
    }

    // Bas niveau : gameName + tag → puuid
    private async Task<string?> GetPuuidAsync(string gameName, string tagLine)
    {
        var url = $"https://europe.api.riotgames.com/riot/account/v1/accounts/by-riot-id/{gameName}/{tagLine}";
        var res = await _http.GetFromJsonAsync<AccountResponse>(url);
        return res?.puuid;
    }

    // Bas niveau : puuid → rank
    public async Task<RiotRankDto?> GetRankFromPuuidAsync(string puuid)
    {
        var url = $"https://euw1.api.riotgames.com/lol/league/v4/entries/by-puuid/{puuid}";
        var leagues = await _http.GetFromJsonAsync<List<LeagueResponse>>(url);

        var soloQueue = leagues?.FirstOrDefault(x => x.QueueType == "RANKED_SOLO_5x5");
        if (soloQueue == null) return null;

        return new RiotRankDto
        {
            Tier = soloQueue.Tier,
            Division = soloQueue.Rank
        };
    }

    private static (string GameName, string TagLine) SplitRiotId(string riotId)
    {
        var parts = riotId.Split('#');
        if (parts.Length != 2) throw new ArgumentException("Format invalide, attendu: name#tag");
        return (parts[0], parts[1]);
    }
}