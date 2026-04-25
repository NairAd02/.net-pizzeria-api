using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pizzeria.API.Modules.Auth;
using Pizzeria.API.Modules.Pizzas.Dtos;
using Pizzeria.API.Modules.Pizzas.Entities;

namespace Pizzeria.API.Modules.Pizzas;

[ApiController]
[Route("api/pizzas")]
[Authorize] // cualquier autenticado puede ver; las mutaciones bajan a Admin.
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
    [Authorize(Roles = Roles.Admin)]
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

    [HttpPost("{id}/images")]
    [Authorize(Roles = Roles.Admin)]
    [RequestSizeLimit(50_000_000)] // tope duro del request (varios archivos); el tamaño por archivo se valida en el service
    public async Task<ActionResult<Pizza>> AddImages(
        string id,
        // Multipart: repetir el campo 'files' por cada imagen. 'altTexts' es opcional
        // y se empareja por índice (files[i] ↔ altTexts[i]); si falta, queda sin alt.
        [FromForm(Name = "files")] List<IFormFile> files,
        [FromForm(Name = "altTexts")] List<string?>? altTexts,
        CancellationToken ct)
    {
        try
        {
            return Ok(await pizzasService.AddImagesAsync(id, files, altTexts, ct));
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

    [HttpDelete("{id}/images/{**objectKey}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<Pizza>> RemoveImage(string id, string objectKey, CancellationToken ct)
    {
        try
        {
            return Ok(await pizzasService.RemoveImageAsync(id, objectKey, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
