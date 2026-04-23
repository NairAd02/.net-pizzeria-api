using Pizzeria.API.Modules.DeliveryPersons.Dtos;
using Pizzeria.API.Modules.DeliveryPersons.Entities;

namespace Pizzeria.API.Modules.DeliveryPersons;

public interface IDeliveryPersonsService
{
    Task<IReadOnlyCollection<DeliveryPerson>> FindAllAsync(CancellationToken ct = default);
    Task<DeliveryPerson?> FindByCodeAsync(string code, CancellationToken ct = default);
    Task<DeliveryPerson> CreateAsync(CreateDeliveryPersonDto dto, CancellationToken ct = default);
}
