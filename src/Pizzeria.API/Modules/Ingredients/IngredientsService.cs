using Microsoft.EntityFrameworkCore;
using Pizzeria.API.Infrastructure.Database;
using Pizzeria.API.Modules.Ingredients.Dtos;
using Pizzeria.API.Modules.Ingredients.Entities;

namespace Pizzeria.API.Modules.Ingredients;

public class IngredientsService(PizzeriaDbContext context) : IIngredientsService
{
    public async Task<IReadOnlyCollection<Ingredient>> FindAllAsync(CancellationToken ct = default)
    {
        var ingredients = await context.Ingredients
            .AsNoTracking()
            .OrderBy(i => i.Code)
            .ToListAsync(ct);
        return ingredients.AsReadOnly();
    }

    public Task<Ingredient?> FindByCodeAsync(string code, CancellationToken ct = default) =>
        context.Ingredients
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Code == code, ct);

    public async Task<Ingredient> CreateAsync(CreateIngredientDto dto, CancellationToken ct = default)
    {
        var exists = await context.Ingredients.AnyAsync(i => i.Code == dto.Code, ct);
        if (exists)
        {
            throw new InvalidOperationException($"Ingredient with code '{dto.Code}' already exists.");
        }

        var ingredient = new Ingredient
        {
            Code = dto.Code,
            Name = dto.Name,
            Stock = dto.Stock,
            PricePerUnit = dto.PricePerUnit,
            Supplier = dto.Supplier,
        };

        context.Ingredients.Add(ingredient);
        await context.SaveChangesAsync(ct);
        return ingredient;
    }

    public async Task<Ingredient> AddStockAsync(string code, decimal quantity, CancellationToken ct = default)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity to add must be positive.", nameof(quantity));
        }

        var ingredient = await context.Ingredients.FirstOrDefaultAsync(i => i.Code == code, ct)
            ?? throw new KeyNotFoundException($"Ingredient '{code}' not found.");

        ingredient.Stock += quantity;
        await context.SaveChangesAsync(ct);
        return ingredient;
    }
}
