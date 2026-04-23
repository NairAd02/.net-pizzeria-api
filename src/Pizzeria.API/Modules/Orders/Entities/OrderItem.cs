namespace Pizzeria.API.Modules.Orders.Entities;

public class OrderItem
{
    public required string PizzaId { get; set; }
    public int Quantity { get; set; } = 1;
}
