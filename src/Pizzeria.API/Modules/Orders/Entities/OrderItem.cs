namespace Pizzeria.API.Modules.Orders.Entities;

public class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OrderId { get; set; } = string.Empty;
    public required string PizzaId { get; set; }
    public int Quantity { get; set; } = 1;
}
