using DocumentAI.DTOs.Requests;
using DocumentAI.DTOs.Responses;
using DocumentAI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentAI.Controllers;

[ApiController]
[Authorize]
[Route("api/user-settings")]
[Produces("application/json")]
public sealed class UserSettingsController : ControllerBase
{
    private readonly IUserSettingsService _userSettingsService;

    public UserSettingsController(IUserSettingsService userSettingsService)
    {
        _userSettingsService = userSettingsService;
    }

    [HttpPost("save")]
    [ProducesResponseType(typeof(QuerySettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateQuerySettings(
        [FromBody] UpdateQuerySettingsRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponse { Error = "Validation failed", Message = "Invalid query settings", Timestamp = DateTime.UtcNow, TraceId = HttpContext.TraceIdentifier });
        }

        if (request.ChunkOverlap >= request.ChunkSize)
        {
            return BadRequest(new ErrorResponse { Error = "Validation failed", Message = "ChunkOverlap must be smaller than ChunkSize", Timestamp = DateTime.UtcNow, TraceId = HttpContext.TraceIdentifier });
        }

        var userId = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var settings = await _userSettingsService.UpdateQuerySettingsAsync(userId, request, cancellationToken);
        return Ok(settings);
    }
}
