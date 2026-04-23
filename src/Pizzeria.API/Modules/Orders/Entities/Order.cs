namespace Pizzeria.API.Modules.Orders.Entities;

public class Order
{
    public required string Id { get; set; }
    public required string CustomerName { get; set; }
    public required string CustomerPhone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public List<OrderItem> Items { get; set; } = new();
    public string? AssignedDeliveryPersonCode { get; set; }
}
