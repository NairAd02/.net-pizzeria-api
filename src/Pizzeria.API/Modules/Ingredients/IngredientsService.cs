using System.Collections.Concurrent;
using Pizzeria.API.Modules.Ingredients.Dtos;
using Pizzeria.API.Modules.Ingredients.Entities;

namespace Pizzeria.API.Modules.Ingredients;

public class IngredientsService : IIngredientsService
{
    private readonly ConcurrentDictionary<string, Ingredient> _store = new();

    public IReadOnlyCollection<Ingredient> FindAll() => _store.Values.ToList().AsReadOnly();

    public Ingredient? FindByCode(string code) =>
        _store.TryGetValue(code, out var ingredient) ? ingredient : null;

    public Ingredient Create(CreateIngredientDto dto)
    {
        var ingredient = new Ingredient
        {
            Code = dto.Code,
            Name = dto.Name,
            Stock = dto.Stock,
            PricePerUnit = dto.PricePerUnit,
            Supplier = dto.Supplier,
        };

        if (!_store.TryAdd(dto.Code, ingredient))
        {
            throw new InvalidOperationException($"Ingredient with code '{dto.Code}' already exists.");
        }

        return ingredient;
    }

    public Ingredient AddStock(string code, decimal quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity to add must be positive.", nameof(quantity));
        }

        var ingredient = FindByCode(code)
            ?? throw new KeyNotFoundException($"Ingredient '{code}' not found.");

        ingredient.Stock += quantity;
        return ingredient;
    }

    public bool HasStock(string code, decimal requiredQuantity)
    {
        var ingredient = FindByCode(code);
        return ingredient is not null && ingredient.Stock >= requiredQuantity;
    }

    public void DecrementStock(string code, decimal quantity)
    {
        var ingredient = FindByCode(code)
            ?? throw new KeyNotFoundException($"Ingredient '{code}' not found.");

        if (ingredient.Stock < quantity)
        {
            throw new InvalidOperationException(
                $"Not enough stock for ingredient '{code}'. Available: {ingredient.Stock}, required: {quantity}.");
        }

        ingredient.Stock -= quantity;
    }
}
