using Microsoft.AspNetCore.Mvc;
using Custom5v5.Api.Contracts.Matches;
using Custom5v5.Application.Matches;

namespace Custom5v5.Api.Controllers;

[ApiController]
[Route("api/matches")]
public sealed class MatchesController : ControllerBase
{
    private readonly MatchProcessor _processor;

    public MatchesController(MatchProcessor processor)
    {
        _processor = processor;
    }

    [HttpPost("fake")]
    public async Task<ActionResult<ProcessMatchResponse>> Process([FromBody] ProcessMatchRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.MatchId))
            return BadRequest(new { error = "matchId is required." });

        var trackingId = Guid.NewGuid();

        // Lancement du traitement (synchrone pour l’instant)
        await _processor.ProcessAsync(request.MatchId.Trim());

        return Accepted(new ProcessMatchResponse(
            TrackingId: trackingId,
            MatchId: request.MatchId.Trim(),
            Status: "Processed"
        ));
    }
}