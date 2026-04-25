using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pizzeria.API.Modules.Auth.Dtos;

namespace Pizzeria.API.Modules.Auth;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("sign-up")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> SignUp([FromBody] SignUpDto dto, CancellationToken ct)
    {
        try
        {
            var response = await authService.SignUpAsync(dto, ct);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("sign-in")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> SignIn([FromBody] SignInDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await authService.SignInAsync(dto, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Refresh([FromBody] RefreshDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await authService.RefreshAsync(dto.RefreshToken, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("sign-out")]
    [Authorize]
    public async Task<IActionResult> SignOut([FromBody] RefreshDto dto, CancellationToken ct)
    {
        await authService.SignOutAsync(dto.RefreshToken, ct);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<MeDto>> Me(CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var me = await authService.GetMeAsync(userId, ct);
        return me is null ? Unauthorized() : Ok(me);
    }
}
