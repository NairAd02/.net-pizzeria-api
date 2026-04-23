namespace Pizzeria.API.Modules.Pizzas.Dtos;

public record PizzaCostDto(
    string PizzaId,
    string Name,
    decimal BasePrice,
    decimal IngredientsCost,
    decimal TotalCost);
