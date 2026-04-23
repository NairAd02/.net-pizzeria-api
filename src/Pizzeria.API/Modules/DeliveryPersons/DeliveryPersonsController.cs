using Microsoft.AspNetCore.Mvc;
using Pizzeria.API.Modules.DeliveryPersons.Dtos;
using Pizzeria.API.Modules.DeliveryPersons.Entities;

namespace Pizzeria.API.Modules.DeliveryPersons;

[ApiController]
[Route("api/delivery-persons")]
public class DeliveryPersonsController(IDeliveryPersonsService deliveryPersonsService) : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<DeliveryPerson>> FindAll() =>
        Ok(deliveryPersonsService.FindAll());

    [HttpGet("{code}")]
    public ActionResult<DeliveryPerson> FindByCode(string code)
    {
        var person = deliveryPersonsService.FindByCode(code);
        return person is null ? NotFound() : Ok(person);
    }

    [HttpPost]
    public ActionResult<DeliveryPerson> Create([FromBody] CreateDeliveryPersonDto dto)
    {
        try
        {
            var created = deliveryPersonsService.Create(dto);
            return CreatedAtAction(nameof(FindByCode), new { code = created.Code }, created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
