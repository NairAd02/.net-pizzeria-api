using Microsoft.AspNetCore.Mvc;
using Pizzeria.API.Modules.Ingredients.Dtos;
using Pizzeria.API.Modules.Ingredients.Entities;

namespace Pizzeria.API.Modules.Ingredients;

[ApiController]
[Route("api/ingredients")]
public class IngredientsController(IIngredientsService ingredientsService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Ingredient>>> FindAll(CancellationToken ct) =>
        Ok(await ingredientsService.FindAllAsync(ct));

    [HttpGet("{code}")]
    public async Task<ActionResult<Ingredient>> FindByCode(string code, CancellationToken ct)
    {
        var ingredient = await ingredientsService.FindByCodeAsync(code, ct);
        return ingredient is null ? NotFound() : Ok(ingredient);
    }

    [HttpPost]
    public async Task<ActionResult<Ingredient>> Create([FromBody] CreateIngredientDto dto, CancellationToken ct)
    {
        try
        {
            var created = await ingredientsService.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(FindByCode), new { code = created.Code }, created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPatch("{code}/stock")]
    public async Task<ActionResult<Ingredient>> AddStock(string code, [FromBody] UpdateStockDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await ingredientsService.AddStockAsync(code, dto.Quantity, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
