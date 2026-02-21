using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using System.Net.Http;
using Custom5v5.Application.Matches.Dtos;


namespace Custom5v5.Application.Matches;

public sealed class MatchProvider : IMatchProvider
{
    private const string _RIOT_API = "REDACTED_RIOT_API_KEY";
    private const string _REQUEST_PATH = "/lol/match/v5/matches/";
    private readonly IHttpClientFactory _httpClientFactory;
    
    public MatchProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }
    public Task<MatchDto?> GetFinishedMatchAsync(string matchId)
    {
        if (matchId == "fake")
            return Task.FromResult(CreateSampleFinishedMatch());
        return RetrieveMatchAsync(matchId);
    }

    private async Task<MatchDto> RetrieveMatchAsync(string matchId, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("Riot");

        // BaseAddress = https://europe.api.riotgames.com
        var url = $"{_REQUEST_PATH}{Uri.EscapeDataString(matchId)}?api_key={Uri.EscapeDataString(_RIOT_API)}";

        using var response = await client.GetAsync(url, ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new KeyNotFoundException($"Match introuvable: {matchId}");

        if (response.StatusCode == (HttpStatusCode)429)
            throw new HttpRequestException("Rate limit Riot (429). Backoff requis.");

        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new HttpRequestException("Forbidden (403). API key invalide/expirée.");

        response.EnsureSuccessStatusCode();

        var match = await response.Content.ReadFromJsonAsync<MatchDto>(cancellationToken: ct);
        return match ?? throw new InvalidOperationException("Réponse Riot vide ou JSON invalide.");
    }
    
    // Exemple "vraie game" (custom) basée sur ton screen
    // Durée: 29:36 = 1776s

    public static MatchDto CreateSampleFinishedMatch()
    {
        var start = DateTimeOffset.Parse("2026-02-18T20:00:00Z").ToUnixTimeMilliseconds();
        var durationSeconds = 29 * 60 + 36;
        var end = start + durationSeconds * 1000L;

        return new MatchDto
        {
            Metadata = new MetadataDto
            {
                DataVersion = "2",
                MatchId = "EUW1_9999999999",
                Participants =
                {
                    "puuid_goat", "puuid_wakanda", "puuid_le93", "puuid_karl", "puuid_getyo",
                    "puuid_chen", "puuid_oda", "puuid_winter", "puuid_unreal", "puuid_zhou"
                }
            },

            Info = new InfoDto
            {
                EndOfGameResult = "GameComplete",
                GameCreation = start - 30_000,               // loading approx
                GameStartTimestamp = start,
                GameEndTimestamp = end,
                GameDuration = durationSeconds,
                GameId = 1234567890123,
                GameMode = "CLASSIC",
                GameType = "CUSTOM_GAME",
                GameName = "CUSTOM_5V5",
                GameVersion = "14.3.1",
                MapId = 11,
                PlatformId = "EUW1",
                QueueId = 0,
                TournamentCode = "EUW1-AAAA-BBBB-CCCC",

                Participants =
                {
                    // TEAM 100 (lose)
                    // Support: faible CS, grosse vision, peu de gold
                    new ParticipantDto { ParticipantId = 1, TeamId = 100, TeamPosition="UTILITY",
                        SummonerName="LE GOAT 93", RiotIdGameName="LE GOAT 93", RiotIdTagline="EUW",
                        Puuid="puuid_goat", ChampionName="Renata", ChampionId=888,
                        Kills=1, Deaths=7, Assists=14, TotalMinionsKilled=24, NeutralMinionsKilled=0,
                        GoldEarned=8200, GoldSpent=8000, ChampLevel=12, VisionScore=45,
                        WardsPlaced=24, WardsKilled=5, VisionWardsBoughtInGame=8,
                        Item0=3117, Item1=3190, Item2=3107, Item3=3011, Item4=0, Item5=0, Item6=3364,
                        Win=false },

                    // Jungle: neutral CS élevé, impact correct mais équipe perd
                    new ParticipantDto { ParticipantId = 2, TeamId = 100, TeamPosition="JUNGLE",
                        SummonerName="WakandaForeverHZ", RiotIdGameName="WakandaForeverHZ", RiotIdTagline="EUW",
                        Puuid="puuid_wakanda", ChampionName="LeeSin", ChampionId=64,
                        Kills=6, Deaths=4, Assists=8, TotalMinionsKilled=62, NeutralMinionsKilled=138,
                        GoldEarned=11850, GoldSpent=11600, ChampLevel=15, VisionScore=28,
                        WardsPlaced=9, WardsKilled=7, VisionWardsBoughtInGame=3,
                        Item0=6692, Item1=3047, Item2=3071, Item3=6333, Item4=1037, Item5=0, Item6=3364,
                        Win=false },

                    // Mid: bon farm, bon gold, bon KDA mais perd
                    new ParticipantDto { ParticipantId = 3, TeamId = 100, TeamPosition="MIDDLE",
                        SummonerName="LE93 C EST BIEN", RiotIdGameName="LE93 C EST BIEN", RiotIdTagline="EUW",
                        Puuid="puuid_le93", ChampionName="Viktor", ChampionId=112,
                        Kills=9, Deaths=4, Assists=6, TotalMinionsKilled=226, NeutralMinionsKilled=10,
                        GoldEarned=13950, GoldSpent=13700, ChampLevel=16, VisionScore=19,
                        WardsPlaced=10, WardsKilled=2, VisionWardsBoughtInGame=1,
                        Item0=6655, Item1=3020, Item2=3165, Item3=3089, Item4=1058, Item5=0, Item6=3363,
                        Win=false },

                    // Top: farm correct, mais game difficile
                    new ParticipantDto { ParticipantId = 4, TeamId = 100, TeamPosition="TOP",
                        SummonerName="karl marx", RiotIdGameName="karl marx", RiotIdTagline="EUW",
                        Puuid="puuid_karl", ChampionName="Garen", ChampionId=86,
                        Kills=2, Deaths=6, Assists=3, TotalMinionsKilled=198, NeutralMinionsKilled=6,
                        GoldEarned=10150, GoldSpent=9950, ChampLevel=15, VisionScore=14,
                        WardsPlaced=7, WardsKilled=1, VisionWardsBoughtInGame=1,
                        Item0=6631, Item1=3111, Item2=3047, Item3=3075, Item4=0, Item5=0, Item6=3340,
                        Win=false },

                    // ADC: gros CS, gold ok, peut perdre quand même
                    new ParticipantDto { ParticipantId = 5, TeamId = 100, TeamPosition="BOTTOM",
                        SummonerName="getyo5exyon", RiotIdGameName="getyo5exyon", RiotIdTagline="EUW",
                        Puuid="puuid_getyo", ChampionName="Jinx", ChampionId=222,
                        Kills=7, Deaths=5, Assists=6, TotalMinionsKilled=242, NeutralMinionsKilled=6,
                        GoldEarned=13200, GoldSpent=12900, ChampLevel=15, VisionScore=16,
                        WardsPlaced=9, WardsKilled=2, VisionWardsBoughtInGame=1,
                        Item0=6672, Item1=3006, Item2=3085, Item3=3031, Item4=0, Item5=0, Item6=3363,
                        Win=false },

                    // TEAM 200 (win)
                    // Top: gagnant, bon CS, bon level
                    new ParticipantDto { ParticipantId = 6, TeamId = 200, TeamPosition="TOP",
                        SummonerName="Chen Yongfeng", RiotIdGameName="Chen Yongfeng", RiotIdTagline="EUW",
                        Puuid="puuid_chen", ChampionName="Aatrox", ChampionId=266,
                        Kills=5, Deaths=3, Assists=7, TotalMinionsKilled=238, NeutralMinionsKilled=8,
                        GoldEarned=14100, GoldSpent=13900, ChampLevel=17, VisionScore=16,
                        WardsPlaced=8, WardsKilled=2, VisionWardsBoughtInGame=1,
                        Item0=6630, Item1=3047, Item2=3071, Item3=6333, Item4=3065, Item5=0, Item6=3340,
                        Win=true },

                    // Jungle: impact + neutral CS + vision ok
                    new ParticipantDto { ParticipantId = 7, TeamId = 200, TeamPosition="JUNGLE",
                        SummonerName="ODA NOBUNAGA", RiotIdGameName="ODA NOBUNAGA", RiotIdTagline="EUW",
                        Puuid="puuid_oda", ChampionName="Viego", ChampionId=234,
                        Kills=8, Deaths=3, Assists=10, TotalMinionsKilled=54, NeutralMinionsKilled=162,
                        GoldEarned=14550, GoldSpent=14300, ChampLevel=16, VisionScore=30,
                        WardsPlaced=10, WardsKilled=8, VisionWardsBoughtInGame=3,
                        Item0=6691, Item1=3047, Item2=3071, Item3=6333, Item4=1036, Item5=0, Item6=3364,
                        Win=true },

                    // Mid: bon score, bon CS/gold
                    new ParticipantDto { ParticipantId = 8, TeamId = 200, TeamPosition="MIDDLE",
                        SummonerName="winter soldier", RiotIdGameName="winter soldier", RiotIdTagline="EUW",
                        Puuid="puuid_winter", ChampionName="Ahri", ChampionId=103,
                        Kills=10, Deaths=4, Assists=9, TotalMinionsKilled=231, NeutralMinionsKilled=10,
                        GoldEarned=15100, GoldSpent=14900, ChampLevel=17, VisionScore=21,
                        WardsPlaced=10, WardsKilled=3, VisionWardsBoughtInGame=1,
                        Item0=6655, Item1=3020, Item2=4645, Item3=3089, Item4=3157, Item5=0, Item6=3363,
                        Win=true },

                    // ADC: carry de win, bon CS/gold
                    new ParticipantDto { ParticipantId = 9, TeamId = 200, TeamPosition="BOTTOM",
                        SummonerName="UnrealFrog", RiotIdGameName="UnrealFrog", RiotIdTagline="EUW",
                        Puuid="puuid_unreal", ChampionName="KaiSa", ChampionId=145,
                        Kills=9, Deaths=4, Assists=6, TotalMinionsKilled=248, NeutralMinionsKilled=4,
                        GoldEarned=14800, GoldSpent=14600, ChampLevel=16, VisionScore=14,
                        WardsPlaced=8, WardsKilled=1, VisionWardsBoughtInGame=1,
                        Item0=6672, Item1=3006, Item2=3124, Item3=3085, Item4=1055, Item5=0, Item6=3363,
                        Win=true },

                    // Support: grosse vision, peu de CS
                    new ParticipantDto { ParticipantId = 10, TeamId = 200, TeamPosition="UTILITY",
                        SummonerName="Zhou Yang Bo", RiotIdGameName="Zhou Yang Bo", RiotIdTagline="EUW",
                        Puuid="puuid_zhou", ChampionName="Nautilus", ChampionId=111,
                        Kills=1, Deaths=5, Assists=18, TotalMinionsKilled=27, NeutralMinionsKilled=0,
                        GoldEarned=8800, GoldSpent=8600, ChampLevel=13, VisionScore=50,
                        WardsPlaced=26, WardsKilled=6, VisionWardsBoughtInGame=8,
                        Item0=3860, Item1=3117, Item2=3190, Item3=3050, Item4=0, Item5=0, Item6=3364,
                        Win=true },
                },
                
                Teams =
                {
                    new TeamDto
                    {
                        TeamId = 100,
                        Win = false,
                        Bans =
                        {
                            new BanDto { ChampionId = 157, PickTurn = 1 }, // Yasuo
                            new BanDto { ChampionId = 238, PickTurn = 2 }, // Zed
                            new BanDto { ChampionId = 245, PickTurn = 3 }, // Ekko
                            new BanDto { ChampionId = 84, PickTurn = 4 },  // Akali
                            new BanDto { ChampionId = 498, PickTurn = 5 }, // Xayah
                        },
                        Objectives = new ObjectivesDto
                        {
                            Baron = new ObjectiveDto { First = false, Kills = 0 },
                            Dragon = new ObjectiveDto { First = false, Kills = 0 },
                            RiftHerald = new ObjectiveDto { First = false, Kills = 1 },
                            Tower = new ObjectiveDto { First = false, Kills = 2 },
                            Inhibitor = new ObjectiveDto { First = false, Kills = 0 },
                            Champion = new ObjectiveDto { First = true, Kills = 27 },
                        }
                    },
                    new TeamDto
                    {
                        TeamId = 200,
                        Win = true,
                        Bans =
                        {
                            new BanDto { ChampionId = 53, PickTurn = 6 },   // Blitz
                            new BanDto { ChampionId = 432, PickTurn = 7 },  // Bard
                            new BanDto { ChampionId = 350, PickTurn = 8 },  // Yuumi
                            new BanDto { ChampionId = 25, PickTurn = 9 },   // Morgana
                            new BanDto { ChampionId = 89, PickTurn = 10 },  // Leona
                        },
                        Objectives = new ObjectivesDto
                        {
                            Baron = new ObjectiveDto { First = true, Kills = 1 },
                            Dragon = new ObjectiveDto { First = true, Kills = 3 },
                            RiftHerald = new ObjectiveDto { First = false, Kills = 0 },
                            Tower = new ObjectiveDto { First = true, Kills = 9 },
                            Inhibitor = new ObjectiveDto { First = true, Kills = 4 },
                            Champion = new ObjectiveDto { First = false, Kills = 29 },
                        }
                    }
                }
            }
        };
    }

}
