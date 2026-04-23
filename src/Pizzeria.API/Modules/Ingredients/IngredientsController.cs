using Microsoft.AspNetCore.Mvc;
using Pizzeria.API.Modules.Ingredients.Dtos;
using Pizzeria.API.Modules.Ingredients.Entities;

namespace Pizzeria.API.Modules.Ingredients;

[ApiController]
[Route("api/ingredients")]
public class IngredientsController(IIngredientsService ingredientsService) : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<Ingredient>> FindAll() =>
        Ok(ingredientsService.FindAll());

    [HttpGet("{code}")]
    public ActionResult<Ingredient> FindByCode(string code)
    {
        var ingredient = ingredientsService.FindByCode(code);
        return ingredient is null ? NotFound() : Ok(ingredient);
    }

    [HttpPost]
    public ActionResult<Ingredient> Create([FromBody] CreateIngredientDto dto)
    {
        try
        {
            var created = ingredientsService.Create(dto);
            return CreatedAtAction(nameof(FindByCode), new { code = created.Code }, created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPatch("{code}/stock")]
    public ActionResult<Ingredient> AddStock(string code, [FromBody] UpdateStockDto dto)
    {
        try
        {
            return Ok(ingredientsService.AddStock(code, dto.Quantity));
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
