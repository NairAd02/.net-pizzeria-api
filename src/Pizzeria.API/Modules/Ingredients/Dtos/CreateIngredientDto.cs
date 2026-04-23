namespace Pizzeria.API.Modules.Ingredients.Dtos;

public record CreateIngredientDto(
    string Code,
    string Name,
    decimal Stock,
    decimal PricePerUnit,
    string? Supplier);
