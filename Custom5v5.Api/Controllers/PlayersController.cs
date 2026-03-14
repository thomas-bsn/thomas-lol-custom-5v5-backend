using Custom5v5.Application.DTOs.Players;
using Custom5v5.Application.Interfaces;  // ← IPlayerService

namespace Custom5v5.Api.Controllers;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("players")]
public class PlayersController : ControllerBase
{
    private readonly IPlayerService _service;  // ← interface

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
}