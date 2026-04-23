namespace Pizzeria.API.Modules.Ingredients.Entities;

public class Ingredient
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public decimal Stock { get; set; }
    public decimal PricePerUnit { get; set; }
    public string? Supplier { get; set; }
}
