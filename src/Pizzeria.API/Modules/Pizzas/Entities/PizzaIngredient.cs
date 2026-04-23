namespace Pizzeria.API.Modules.Pizzas.Entities;

public class PizzaIngredient
{
    public required string IngredientCode { get; set; }
    public decimal Quantity { get; set; }
}
