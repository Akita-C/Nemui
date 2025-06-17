using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateDrawRoomAsync(
        [FromForm] CreateDrawRoom request,
        CancellationToken cancellationToken = default) =>
        await ExecuteWithAuthenticationAsync(async userId =>
        {
            var roomId = await gameService.CreateRoomAsync(
                new DrawHost { HostId = GetCurrentUserId().ToString(), HostName = GetCurrentUserName() }, 
                request, 
                cancellationToken
            );
            if (roomId == Guid.Empty)
                return BadRequest(ErrorResponse.Create("Failed to create draw room"));
            return Ok(ApiResponse<Guid>.SuccessResult(roomId,"Create draw room successfully"));
        }, "create draw room", logger);
}