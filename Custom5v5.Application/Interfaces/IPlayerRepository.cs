namespace Custom5v5.Application.Interfaces;

using Custom5v5.Application.DTOs.Players;

public interface IPlayerRepository
{
    Task<List<PlayerDto>> GetAllAsync();
    Task<bool> ExistsByRiotIdAsync(string riotId);
    Task<PlayerDto> AddAsync(PlayerDto player);
}