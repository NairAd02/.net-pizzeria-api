namespace Pizzeria.API.Modules.Pizzas.Dtos;

public record CreatePizzaDto(
    string Name,
    decimal BasePrice,
    List<PizzaIngredientDto> Ingredients);

public record PizzaIngredientDto(string IngredientCode, decimal Quantity);
