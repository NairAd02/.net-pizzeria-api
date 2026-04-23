using Microsoft.EntityFrameworkCore;
using Pizzeria.API.Infrastructure.Database;
using Pizzeria.API.Modules.Pizzas.Dtos;
using Pizzeria.API.Modules.Pizzas.Entities;

namespace Pizzeria.API.Modules.Pizzas;

public class PizzasService(PizzeriaDbContext context) : IPizzasService
{
    public async Task<IReadOnlyCollection<Pizza>> FindAllAsync(CancellationToken ct = default)
    {
        var pizzas = await context.Pizzas
            .AsNoTracking()
            .Include(p => p.Ingredients)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
        return pizzas.AsReadOnly();
    }

    public Task<Pizza?> FindByIdAsync(string id, CancellationToken ct = default) =>
        context.Pizzas
            .AsNoTracking()
            .Include(p => p.Ingredients)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Pizza> CreateAsync(CreatePizzaDto dto, CancellationToken ct = default)
    {
        if (dto.Ingredients is null || dto.Ingredients.Count == 0)
        {
            throw new ArgumentException("A pizza must declare at least one ingredient.");
        }

        // Validamos en una sola consulta que todos los ingredientes existan.
        var codes = dto.Ingredients.Select(i => i.IngredientCode).Distinct().ToList();
        var existingCodes = await context.Ingredients
            .Where(i => codes.Contains(i.Code))
            .Select(i => i.Code)
            .ToListAsync(ct);

        var missing = codes.Except(existingCodes).ToList();
        if (missing.Count > 0)
        {
            throw new KeyNotFoundException(
                $"Ingredients not found: {string.Join(", ", missing)}.");
        }

        var pizza = new Pizza
        {
            Id = Guid.NewGuid().ToString(),
            Name = dto.Name,
            BasePrice = dto.BasePrice,
            Ingredients = dto.Ingredients
                .Select(i => new PizzaIngredient
                {
                    IngredientCode = i.IngredientCode,
                    Quantity = i.Quantity,
                })
                .ToList(),
        };

        context.Pizzas.Add(pizza);
        await context.SaveChangesAsync(ct);
        return pizza;
    }

    public async Task<PizzaCostDto> CalculateCostAsync(string id, CancellationToken ct = default)
    {
        var pizza = await context.Pizzas
            .AsNoTracking()
            .Include(p => p.Ingredients)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new KeyNotFoundException($"Pizza '{id}' not found.");

        var codes = pizza.Ingredients.Select(pi => pi.IngredientCode).ToList();
        var prices = await context.Ingredients
            .Where(i => codes.Contains(i.Code))
            .ToDictionaryAsync(i => i.Code, i => i.PricePerUnit, ct);

        decimal ingredientsCost = 0m;
        foreach (var item in pizza.Ingredients)
        {
            if (!prices.TryGetValue(item.IngredientCode, out var pricePerUnit))
            {
                throw new InvalidOperationException(
                    $"Ingredient '{item.IngredientCode}' no longer exists.");
            }
            ingredientsCost += pricePerUnit * item.Quantity;
        }

        return new PizzaCostDto(
            pizza.Id,
            pizza.Name,
            pizza.BasePrice,
            ingredientsCost,
            pizza.BasePrice + ingredientsCost);
    }
}
