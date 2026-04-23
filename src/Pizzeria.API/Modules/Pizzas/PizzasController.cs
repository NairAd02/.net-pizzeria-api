using Microsoft.AspNetCore.Mvc;
using Pizzeria.API.Modules.Pizzas.Dtos;
using Pizzeria.API.Modules.Pizzas.Entities;

namespace Pizzeria.API.Modules.Pizzas;

[ApiController]
[Route("api/pizzas")]
public class PizzasController(IPizzasService pizzasService) : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<Pizza>> FindAll() =>
        Ok(pizzasService.FindAll());

    [HttpGet("{id}")]
    public ActionResult<Pizza> FindById(string id)
    {
        var pizza = pizzasService.FindById(id);
        return pizza is null ? NotFound() : Ok(pizza);
    }

    [HttpPost]
    public ActionResult<Pizza> Create([FromBody] CreatePizzaDto dto)
    {
        try
        {
            var created = pizzasService.Create(dto);
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
    public ActionResult<PizzaCostDto> CalculateCost(string id)
    {
        try
        {
            return Ok(pizzasService.CalculateCost(id));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
