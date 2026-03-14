// Custom5v5.Infrastructure/Data/Repositories/PlayerRepository.cs
using Custom5v5.Application.DTOs.Players;
using Custom5v5.Application.Interfaces;
using Custom5v5.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Custom5v5.Infrastructure.Data.Repositories;

public class PlayerRepository : IPlayerRepository
{
    private readonly AppDbContext _db;

    public PlayerRepository(AppDbContext db) => _db = db;

    public async Task<List<PlayerDto>> GetAllAsync()
    {
        return await _db.Players
            .Select(p => ToDto(p))
            .ToListAsync();
    }

    public Task<bool> ExistsByRiotIdAsync(string riotId) =>
        _db.Players.AnyAsync(p => p.RiotId == riotId);

    public async Task<PlayerDto> AddAsync(PlayerDto dto)
    {
        var player = new Player
        {
            Prenom = dto.Prenom,
            RiotId = dto.RiotId,
            PUUID = dto.PUUID,
            RankTier = dto.RankTier,
            RankDivision = dto.RankDivision,
            CreatedAt = DateTime.UtcNow
        };

        _db.Players.Add(player);
        await _db.SaveChangesAsync();

        dto.Id = player.Id;
        return dto;
    }

    private static PlayerDto ToDto(Player p) => new()
    {
        Id = p.Id,
        Prenom = p.Prenom,
        RiotId = p.RiotId,
        PUUID = p.PUUID,
        RankTier = p.RankTier,
        RankDivision = p.RankDivision
    };
}