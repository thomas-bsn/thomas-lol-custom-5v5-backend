namespace Custom5v5.Application.DTOs.Players;

public class PlayerResponse
{
    public int Id { get; set; }
    public string Prenom { get; set; } = null!;
    public string RiotId { get; set; } = null!;
}