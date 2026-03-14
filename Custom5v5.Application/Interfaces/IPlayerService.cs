using Custom5v5.Application.DTOs.Players;

namespace Custom5v5.Application.Interfaces;

public interface IPlayerService
{
    Task<List<PlayerDto>> GetPlayersAsync();
    Task<PlayerResponse> CreatePlayerAsync(string prenom, string riotId);
}