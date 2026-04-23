namespace Pizzeria.API.Modules.Pizzas.Entities;

public class PizzaIngredient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PizzaId { get; set; } = string.Empty;
    public required string IngredientCode { get; set; }
    public decimal Quantity { get; set; }
}
