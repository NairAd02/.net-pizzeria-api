using Microsoft.AspNetCore.Mvc;
using Pizzeria.API.Modules.Pizzas.Dtos;
using Pizzeria.API.Modules.Pizzas.Entities;

namespace Pizzeria.API.Modules.Pizzas;

[ApiController]
[Route("api/pizzas")]
public class PizzasController(IPizzasService pizzasService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Pizza>>> FindAll(CancellationToken ct) =>
        Ok(await pizzasService.FindAllAsync(ct));

    [HttpGet("{id}")]
    public async Task<ActionResult<Pizza>> FindById(string id, CancellationToken ct)
    {
        var pizza = await pizzasService.FindByIdAsync(id, ct);
        return pizza is null ? NotFound() : Ok(pizza);
    }

    [HttpPost]
    public async Task<ActionResult<Pizza>> Create([FromBody] CreatePizzaDto dto, CancellationToken ct)
    {
        try
        {
            var created = await pizzasService.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(FindById), new { id = created.Id }, created);
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}/cost")]
    public async Task<ActionResult<PizzaCostDto>> CalculateCost(string id, CancellationToken ct)
    {
        try
        {
            return Ok(await pizzasService.CalculateCostAsync(id, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
