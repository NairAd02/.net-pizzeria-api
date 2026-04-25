using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pizzeria.API.Modules.Auth;
using Pizzeria.API.Modules.Orders.Dtos;
using Pizzeria.API.Modules.Orders.Entities;

namespace Pizzeria.API.Modules.Orders;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController(IOrdersService ordersService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<IEnumerable<Order>>> FindAll(CancellationToken ct) =>
        Ok(await ordersService.FindAllAsync(ct));

    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> FindById(string id, CancellationToken ct)
    {
        var order = await ordersService.FindByIdAsync(id, ct);
        if (order is null)
        {
            return NotFound();
        }

        // Admin ve cualquier pedido; Client solo los suyos.
        if (!User.IsInRole(Roles.Admin))
        {
            if (!TryGetCurrentUserId(out var userId) || order.CustomerUserId != userId)
            {
                return Forbid();
            }
        }

        return Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<Order>> Create([FromBody] CreateOrderDto dto, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var order = await ordersService.CreateAsync(dto, userId, ct);
            return CreatedAtAction(nameof(FindById), new { id = order.Id }, order);
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<Order>> UpdateStatus(string id, [FromBody] UpdateOrderStatusDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await ordersService.UpdateStatusAsync(id, dto.Status, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }
}
