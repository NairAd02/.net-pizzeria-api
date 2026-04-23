namespace Pizzeria.API.Modules.Pizzas.Entities;

public class Pizza
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public decimal BasePrice { get; set; }
    public List<PizzaIngredient> Ingredients { get; set; } = new();
}
