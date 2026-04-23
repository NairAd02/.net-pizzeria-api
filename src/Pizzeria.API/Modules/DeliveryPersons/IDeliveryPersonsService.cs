using Pizzeria.API.Modules.DeliveryPersons.Dtos;
using Pizzeria.API.Modules.DeliveryPersons.Entities;

namespace Pizzeria.API.Modules.DeliveryPersons;

public interface IDeliveryPersonsService
{
    IReadOnlyCollection<DeliveryPerson> FindAll();
    DeliveryPerson? FindByCode(string code);
    DeliveryPerson Create(CreateDeliveryPersonDto dto);
    DeliveryPerson? FindAvailable();
    DeliveryPerson MarkBusy(string code);
    DeliveryPerson MarkAvailable(string code);
}
