namespace Pizzeria.API.Modules.Orders.Dtos;

public record CreateOrderDto(
    string CustomerName,
    string CustomerPhone,
    List<OrderItemDto> Items);

public record OrderItemDto(string PizzaId, int Quantity);
