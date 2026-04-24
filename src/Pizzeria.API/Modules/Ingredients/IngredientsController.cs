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

    [HttpPost("{code}/images")]
    [RequestSizeLimit(50_000_000)] // tope duro del request (varios archivos); el tamaño por archivo se valida en el service
    public async Task<ActionResult<Ingredient>> AddImages(
        string code,
        // Multipart: repetir el campo 'files' por cada imagen. 'altTexts' es opcional
        // y se empareja por índice (files[i] ↔ altTexts[i]); si falta, queda sin alt.
        [FromForm(Name = "files")] List<IFormFile> files,
        [FromForm(Name = "altTexts")] List<string?>? altTexts,
        CancellationToken ct)
    {
        try
        {
            return Ok(await ingredientsService.AddImagesAsync(code, files, altTexts, ct));
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

    [HttpDelete("{code}/images/{**objectKey}")]
    public async Task<ActionResult<Ingredient>> RemoveImage(string code, string objectKey, CancellationToken ct)
    {
        try
        {
            return Ok(await ingredientsService.RemoveImageAsync(code, objectKey, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
