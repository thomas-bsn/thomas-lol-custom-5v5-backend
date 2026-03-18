using Custom5v5.Application.DTOs.Players;
using Custom5v5.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Custom5v5.Api.Controllers;

[ApiController]
[Route("players")]
public class PlayersController : ControllerBase
{
    private readonly IPlayerService _service;

    public PlayersController(IPlayerService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetPlayers()
    {
        var players = await _service.GetPlayersAsync();
        return Ok(players);
    }

    [HttpPost]
    public async Task<ActionResult<PlayerResponse>> AddPlayer(CreatePlayerRequest request)
    {
        var player = await _service.CreatePlayerAsync(request.Prenom, request.RiotId);
        return Ok(player);
    }

    // Lier son compte Discord à un player
    [HttpPost("link")]
    [Authorize]
    public async Task<IActionResult> LinkPlayer([FromBody] LinkPlayerRequest request)
    {
        var discordUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var username = User.FindFirstValue(ClaimTypes.Name);
        var avatarUrl = User.FindFirstValue("avatar_url");
    
        if (string.IsNullOrWhiteSpace(discordUserId)) return Unauthorized();

        await _service.LinkPlayerAsync(discordUserId, request.PlayerId, username, avatarUrl);
        return Ok(new { ok = true });
    }

    // Récupérer le player lié à son compte
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMyPlayer()
    {
        var discordUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(discordUserId)) return Unauthorized();

        var player = await _service.GetPlayerByDiscordIdAsync(discordUserId);
        if (player == null) return NotFound(new { error = "no_linked_player" });

        return Ok(player);
    }

    [HttpPost("refresh-ranks")]
    public async Task<IActionResult> RefreshRanks()
    {
        await _service.RefreshAllRanksAsync();
        return Ok("Ranks mis à jour");
    }
    
    [HttpDelete("peak")]
    [Authorize]
    public async Task<IActionResult> DeletePeak()
    {
        var discordUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(discordUserId)) return Unauthorized();

        await _service.UpdatePeakAsync(discordUserId, null, null, null, 0);
        return Ok(new { ok = true });
    }
    
    [HttpPost("peak")]
    [Authorize]
    public async Task<IActionResult> UpdatePeak([FromBody] UpdatePeakRequest request)
    {
        var discordUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(discordUserId)) return Unauthorized();

        await _service.UpdatePeakAsync(discordUserId, request.PeakTier, request.PeakDivision, request.PeakSeason, request.PeakLp);
        return Ok(new { ok = true });
    }
}

public record LinkPlayerRequest(int PlayerId);