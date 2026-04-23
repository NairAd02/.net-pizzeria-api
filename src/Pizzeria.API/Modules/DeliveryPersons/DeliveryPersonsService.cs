using System.Collections.Concurrent;
using Pizzeria.API.Modules.DeliveryPersons.Dtos;
using Pizzeria.API.Modules.DeliveryPersons.Entities;

namespace Pizzeria.API.Modules.DeliveryPersons;

public class DeliveryPersonsService : IDeliveryPersonsService
{
    private readonly ConcurrentDictionary<string, DeliveryPerson> _store = new();

    public IReadOnlyCollection<DeliveryPerson> FindAll() => _store.Values.ToList().AsReadOnly();

    public DeliveryPerson? FindByCode(string code) =>
        _store.TryGetValue(code, out var person) ? person : null;

    public DeliveryPerson Create(CreateDeliveryPersonDto dto)
    {
        var person = new DeliveryPerson
        {
            Code = dto.Code,
            Name = dto.Name,
            Phone = dto.Phone,
        };

        if (!_store.TryAdd(dto.Code, person))
        {
            throw new InvalidOperationException(
                $"Delivery person with code '{dto.Code}' already exists.");
        }

        return person;
    }

    public DeliveryPerson? FindAvailable() =>
        _store.Values.FirstOrDefault(p => p.Status == DeliveryPersonStatus.Available);

    public DeliveryPerson MarkBusy(string code)
    {
        var person = FindByCode(code)
            ?? throw new KeyNotFoundException($"Delivery person '{code}' not found.");

        person.Status = DeliveryPersonStatus.Busy;
        return person;
    }

    public DeliveryPerson MarkAvailable(string code)
    {
        var person = FindByCode(code)
            ?? throw new KeyNotFoundException($"Delivery person '{code}' not found.");

        person.Status = DeliveryPersonStatus.Available;
        return person;
    }
}
