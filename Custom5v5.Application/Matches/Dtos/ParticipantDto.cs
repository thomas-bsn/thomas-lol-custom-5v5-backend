namespace Custom5v5.Application.Matches.Dtos;

public sealed class ParticipantDto
{
    public int ParticipantId { get; init; }
    public int TeamId { get; init; }

    public string TeamPosition { get; init; } = ""; // TOP/JUNGLE/MIDDLE/BOTTOM/UTILITY
    public string SummonerName { get; init; } = "";
    public string RiotIdGameName { get; init; } = "";
    public string RiotIdTagline { get; init; } = "";
    public string Puuid { get; init; } = "";

    public int ChampionId { get; init; }
    public string ChampionName { get; init; } = "";

    public int ChampLevel { get; init; }

    public int Kills { get; init; }
    public int Deaths { get; init; }
    public int Assists { get; init; }

    public int TotalMinionsKilled { get; init; }
    public int NeutralMinionsKilled { get; init; }

    public int GoldEarned { get; init; }
    public int GoldSpent { get; init; }

    public int VisionScore { get; init; }
    public int WardsPlaced { get; init; }
    public int WardsKilled { get; init; }
    public int VisionWardsBoughtInGame { get; init; }

    // Items (Riot utilise item0..item6)
    public int Item0 { get; init; }
    public int Item1 { get; init; }
    public int Item2 { get; init; }
    public int Item3 { get; init; }
    public int Item4 { get; init; }
    public int Item5 { get; init; }
    public int Item6 { get; init; }

    public bool Win { get; init; }
}