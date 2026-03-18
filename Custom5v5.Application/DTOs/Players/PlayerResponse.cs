namespace Custom5v5.Application.DTOs.Players;

public class PlayerResponse
{
    public int Id { get; set; }
    public string Prenom { get; set; }
    public string RiotId { get; set; }
    public string? RankTier { get; set; }
    public int? RankDivision { get; set; }
    public int? LP { get; set; }
}