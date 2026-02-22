using System.Security.Claims;
using Custom5v5.Api.Contracts;
using Custom5v5.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Custom5v5.Api.Controllers;

[ApiController]
public sealed class PollController : ControllerBase
{
    private readonly PollStore _polls;

    public PollController(PollStore polls)
    {
        _polls = polls;
    }

    // DEV: open poll (no auth for now)
    [HttpPost("/dev/poll/open")]
    public ActionResult<PollDto> Open([FromBody] OpenPollRequest req)
    {
        var poll = _polls.Open(req.DurationHours);
        return Ok(poll);
    }

    [HttpGet("/poll/current")]
    public ActionResult<PollDto> Current()
    {
        var poll = _polls.GetCurrent();
        if (poll is null) return NotFound(new { error = "no_poll" });
        return Ok(poll);
    }

    [HttpPut("/poll/current/ballot")]
    [Authorize]
    public IActionResult Submit([FromBody] SubmitBallotRequest req)
    {
        var voterId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(voterId))
            return Unauthorized();

        _polls.UpsertBallot(voterId, req);
        return Ok(new { ok = true });
    }

    [HttpGet("/poll/current/results")]
    public ActionResult<PollResultsDto> Results()
    {
        return Ok(_polls.GetResults());
    }
}