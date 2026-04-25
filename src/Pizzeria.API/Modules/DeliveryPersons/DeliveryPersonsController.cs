using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pizzeria.API.Modules.Auth;
using Pizzeria.API.Modules.DeliveryPersons.Dtos;
using Pizzeria.API.Modules.DeliveryPersons.Entities;

namespace Pizzeria.API.Modules.DeliveryPersons;

[ApiController]
[Route("api/delivery-persons")]
[Authorize(Roles = Roles.Admin)] // toda la gestión de repartidores es solo para Admin.
public class DeliveryPersonsController(IDeliveryPersonsService deliveryPersonsService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DeliveryPerson>>> FindAll(CancellationToken ct) =>
        Ok(await deliveryPersonsService.FindAllAsync(ct));

    [HttpGet("{code}")]
    public async Task<ActionResult<DeliveryPerson>> FindByCode(string code, CancellationToken ct)
    {
        var person = await deliveryPersonsService.FindByCodeAsync(code, ct);
        return person is null ? NotFound() : Ok(person);
    }

    [HttpPost]
    public async Task<ActionResult<DeliveryPerson>> Create([FromBody] CreateDeliveryPersonDto dto, CancellationToken ct)
    {
        try
        {
            var created = await deliveryPersonsService.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(FindByCode), new { code = created.Code }, created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
