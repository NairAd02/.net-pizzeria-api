using Microsoft.AspNetCore.Mvc;
using Pizzeria.API.Modules.Orders.Dtos;
using Pizzeria.API.Modules.Orders.Entities;

namespace Pizzeria.API.Modules.Orders;

[ApiController]
[Route("api/orders")]
public class OrdersController(IOrdersService ordersService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> FindAll(CancellationToken ct) =>
        Ok(await ordersService.FindAllAsync(ct));

    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> FindById(string id, CancellationToken ct)
    {
        var order = await ordersService.FindByIdAsync(id, ct);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<Order>> Create([FromBody] CreateOrderDto dto, CancellationToken ct)
    {
        try
        {
            var order = await ordersService.CreateAsync(dto, ct);
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
}
