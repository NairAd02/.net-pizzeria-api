using Microsoft.EntityFrameworkCore;
using Pizzeria.API.Infrastructure.Database;
using Pizzeria.API.Modules.DeliveryPersons.Dtos;
using Pizzeria.API.Modules.DeliveryPersons.Entities;

namespace Pizzeria.API.Modules.DeliveryPersons;

public class DeliveryPersonsService(PizzeriaDbContext context) : IDeliveryPersonsService
{
    public async Task<IReadOnlyCollection<DeliveryPerson>> FindAllAsync(CancellationToken ct = default)
    {
        var people = await context.DeliveryPersons
            .AsNoTracking()
            .OrderBy(p => p.Code)
            .ToListAsync(ct);
        return people.AsReadOnly();
    }

    public Task<DeliveryPerson?> FindByCodeAsync(string code, CancellationToken ct = default) =>
        context.DeliveryPersons
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Code == code, ct);

    public async Task<DeliveryPerson> CreateAsync(CreateDeliveryPersonDto dto, CancellationToken ct = default)
    {
        var exists = await context.DeliveryPersons.AnyAsync(p => p.Code == dto.Code, ct);
        if (exists)
        {
            throw new InvalidOperationException(
                $"Delivery person with code '{dto.Code}' already exists.");
        }

        var person = new DeliveryPerson
        {
            Code = dto.Code,
            Name = dto.Name,
            Phone = dto.Phone,
        };

        context.DeliveryPersons.Add(person);
        await context.SaveChangesAsync(ct);
        return person;
    }
}
