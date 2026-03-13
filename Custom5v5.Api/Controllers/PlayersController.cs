using Custom5v5.Application.Services;

namespace Custom5v5.Api.Controllers;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("players")]
public class PlayersController : ControllerBase
{
    private readonly PlayerService _service;

    public PlayersController(PlayerService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetPlayers()
    {
        var players = await _service.GetPlayersAsync();
        return Ok(players);
    }
}