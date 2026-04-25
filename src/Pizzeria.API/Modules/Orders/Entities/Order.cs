namespace Pizzeria.API.Modules.Orders.Entities;

public class Order
{
    public required string Id { get; set; }
    // Id del User que hizo el pedido. Se rellena desde el JWT en el controller,
    // nunca desde el body, para que un Client no pueda colocar pedidos a nombre de otro.
    public required Guid CustomerUserId { get; set; }
    public required string CustomerName { get; set; }
    public required string CustomerPhone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public List<OrderItem> Items { get; set; } = new();
    public string? AssignedDeliveryPersonCode { get; set; }
}
