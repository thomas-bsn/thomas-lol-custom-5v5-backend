namespace Custom5v5.Infrastructure.Entities;

public class Player
{
    public int Id { get; set; }

    public string Prenom { get; set; } = null!;

    public string RiotId { get; set; } = null!;

    public string RankTier { get; set; } = null!;

    public int? RankDivision { get; set; }

    public string? DiscordUsername { get; set; }

    public DateTime CreatedAt { get; set; }
}