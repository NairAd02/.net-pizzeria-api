using Pizzeria.API.Modules.Ingredients.Dtos;
using Pizzeria.API.Modules.Ingredients.Entities;

namespace Pizzeria.API.Modules.Ingredients;

public interface IIngredientsService
{
    Task<IReadOnlyCollection<Ingredient>> FindAllAsync(CancellationToken ct = default);
    Task<Ingredient?> FindByCodeAsync(string code, CancellationToken ct = default);
    Task<Ingredient> CreateAsync(CreateIngredientDto dto, CancellationToken ct = default);
    Task<Ingredient> AddStockAsync(string code, decimal quantity, CancellationToken ct = default);
}
