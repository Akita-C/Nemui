using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Nemui.Application.Services.Games.Draw;
using Nemui.Shared.DTOs.Common;
using Nemui.Shared.DTOs.Games.Draw;

namespace Nemui.Api.Controllers;

[Authorize]
public class DrawGameController(
    IDrawGameService gameService,
    ILogger<DrawGameController> logger
) : BaseApiController
{
    [HttpPost("create")]
    [EnableRateLimiting("DrawGamePolicy")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateDrawRoomAsync(
        [FromForm] CreateDrawRoom request) =>
        await ExecuteWithAuthenticationAsync(async userId =>
        {
            var roomId = await gameService.CreateRoomAsync(
                new DrawHost { HostId = userId.ToString(), HostName = GetCurrentUserName() },
                request
            );
            if (roomId == Guid.Empty)
                return BadRequest(ErrorResponse.Create("Failed to create draw room"));
            var result = await gameService.AddPlayerAsync(roomId, new DrawPlayer(null, userId.ToString(), GetCurrentUserName(), null));
            if (!result)
                return BadRequest(ErrorResponse.Create("Failed to add host to draw room"));
            return Ok(ApiResponse<Guid>.SuccessResult(roomId, "Create draw room successfully"));
        }, "create draw room", logger);

    [HttpGet("room/{roomId}")]
    [ProducesResponseType(typeof(ApiResponse<DrawRoom>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDrawRoomAsync([FromRoute] Guid roomId) =>
        await ExecuteWithAuthenticationAsync(async userId =>
        {
            var room = await gameService.GetRoomAsync(roomId);
            if (room == null)
                return NotFound(ErrorResponse.Create("Draw room not found"));
            return Ok(ApiResponse<DrawRoom>.SuccessResult(room, "Get draw room successfully"));
        }, "get draw room", logger);

    [HttpGet("room/{roomId}/players")]
    [ProducesResponseType(typeof(ApiResponse<List<DrawPlayer>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllPlayersAsync([FromQuery] string playerId, [FromRoute] Guid roomId) =>
        await ExecuteWithAuthenticationAsync(async userId =>
        {
            var players = await gameService.GetAllPlayersAsync(playerId, roomId);
            if (players == null)
                return NotFound(ErrorResponse.Create("Draw room not found"));
            return Ok(ApiResponse<List<DrawPlayer>>.SuccessResult(players!, "Get all players successfully"));
        }, "get all players", logger);
}