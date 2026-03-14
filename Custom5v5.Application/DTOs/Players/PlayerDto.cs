namespace Custom5v5.Application.DTOs.Players;

public class PlayerDto
{
    public int Id { get; set; }
    public string Prenom { get; set; }
    public string RiotId { get; set; }
    public string? PUUID { get; set; }
    public string? RankTier { get; set; }
    public int? RankDivision { get; set; }
}