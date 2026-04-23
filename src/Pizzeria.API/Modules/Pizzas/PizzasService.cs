using System.Collections.Concurrent;
using Pizzeria.API.Modules.Ingredients;
using Pizzeria.API.Modules.Pizzas.Dtos;
using Pizzeria.API.Modules.Pizzas.Entities;

namespace Pizzeria.API.Modules.Pizzas;

public class PizzasService(IIngredientsService ingredientsService) : IPizzasService
{
    private readonly ConcurrentDictionary<string, Pizza> _store = new();

    public IReadOnlyCollection<Pizza> FindAll() => _store.Values.ToList().AsReadOnly();

    public Pizza? FindById(string id) =>
        _store.TryGetValue(id, out var pizza) ? pizza : null;

    public Pizza Create(CreatePizzaDto dto)
    {
        if (dto.Ingredients is null || dto.Ingredients.Count == 0)
        {
            throw new ArgumentException("A pizza must declare at least one ingredient.");
        }

        // Validamos que todos los ingredientes existan antes de persistir la pizza.
        foreach (var item in dto.Ingredients)
        {
            if (ingredientsService.FindByCode(item.IngredientCode) is null)
            {
                throw new KeyNotFoundException($"Ingredient '{item.IngredientCode}' not found.");
            }
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

        _store[pizza.Id] = pizza;
        return pizza;
    }

    public PizzaCostDto CalculateCost(string id)
    {
        var pizza = FindById(id)
            ?? throw new KeyNotFoundException($"Pizza '{id}' not found.");

        decimal ingredientsCost = 0m;
        foreach (var item in pizza.Ingredients)
        {
            var ingredient = ingredientsService.FindByCode(item.IngredientCode)
                ?? throw new InvalidOperationException(
                    $"Ingredient '{item.IngredientCode}' no longer exists.");

            ingredientsCost += ingredient.PricePerUnit * item.Quantity;
        }

        return new PizzaCostDto(
            pizza.Id,
            pizza.Name,
            pizza.BasePrice,
            ingredientsCost,
            pizza.BasePrice + ingredientsCost);
    }
}
