namespace Custom5v5.Application.Services;

using Custom5v5.Infrastructure.Data;
using Custom5v5.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

public class PlayerService
{
    private readonly AppDbContext _db;

    public PlayerService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Player>> GetPlayersAsync()
    {
        return await _db.Players.ToListAsync();
    }
}