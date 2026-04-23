using Pizzeria.API.Modules.Pizzas.Dtos;
using Pizzeria.API.Modules.Pizzas.Entities;

namespace Pizzeria.API.Modules.Pizzas;

public interface IPizzasService
{
    Task<IReadOnlyCollection<Pizza>> FindAllAsync(CancellationToken ct = default);
    Task<Pizza?> FindByIdAsync(string id, CancellationToken ct = default);
    Task<Pizza> CreateAsync(CreatePizzaDto dto, CancellationToken ct = default);
    Task<PizzaCostDto> CalculateCostAsync(string id, CancellationToken ct = default);
    Task<Pizza> AddImageAsync(string id, IFormFile file, string? altText, CancellationToken ct = default);
    Task<Pizza> RemoveImageAsync(string id, string objectKey, CancellationToken ct = default);
}
