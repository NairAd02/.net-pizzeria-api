using Microsoft.AspNetCore.Mvc;
using Pizzeria.API.Modules.Orders.Dtos;
using Pizzeria.API.Modules.Orders.Entities;

namespace Pizzeria.API.Modules.Orders;

[ApiController]
[Route("api/orders")]
public class OrdersController(IOrdersService ordersService) : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<Order>> FindAll() =>
        Ok(ordersService.FindAll());

    [HttpGet("{id}")]
    public ActionResult<Order> FindById(string id)
    {
        var order = ordersService.FindById(id);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost]
    public ActionResult<Order> Create([FromBody] CreateOrderDto dto)
    {
        try
        {
            var order = ordersService.Create(dto);
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
    public ActionResult<Order> UpdateStatus(string id, [FromBody] UpdateOrderStatusDto dto)
    {
        try
        {
            return Ok(ordersService.UpdateStatus(id, dto.Status));
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
