using Custom5v5.Api.Contracts.Tracking;
using Microsoft.AspNetCore.Mvc;

namespace Custom5v5.Api.Controllers;

[ApiController]
[Route("api/tracking")]
public sealed class TrackingController : ControllerBase
{
    // POST /api/tracking/start
    [HttpPost("start")]
    [ProducesResponseType(typeof(StartTrackingResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<StartTrackingResponse> Start([FromBody] StartTrackingRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.TournamentCode))
            return BadRequest("tournamentCode is required.");

        // TODO plus tard:
        // - enregistrer en DB (status=WaitingForStart, nextPollAt=now, etc.)
        // - le worker lira la DB et poll Riot
        
        return Accepted(new StartTrackingResponse
        {
            TrackingId = Guid.NewGuid(),
            TournamentCode = request.TournamentCode.Trim()
        });
    }
}


