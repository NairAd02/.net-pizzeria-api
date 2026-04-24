using Pizzeria.API.Modules.Ingredients.Dtos;
using Pizzeria.API.Modules.Ingredients.Entities;

namespace Pizzeria.API.Modules.Ingredients;

public interface IIngredientsService
{
    Task<IReadOnlyCollection<Ingredient>> FindAllAsync(CancellationToken ct = default);
    Task<Ingredient?> FindByCodeAsync(string code, CancellationToken ct = default);
    Task<Ingredient> CreateAsync(CreateIngredientDto dto, CancellationToken ct = default);
    Task<Ingredient> AddStockAsync(string code, decimal quantity, CancellationToken ct = default);
    Task<Ingredient> AddImagesAsync(
        string code,
        IReadOnlyList<IFormFile> files,
        IReadOnlyList<string?>? altTexts,
        CancellationToken ct = default);
    Task<Ingredient> RemoveImageAsync(string code, string objectKey, CancellationToken ct = default);
}
