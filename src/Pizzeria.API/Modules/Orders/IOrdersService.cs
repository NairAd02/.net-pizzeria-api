using Pizzeria.API.Modules.Orders.Dtos;
using Pizzeria.API.Modules.Orders.Entities;

namespace Pizzeria.API.Modules.Orders;

public interface IOrdersService
{
    IReadOnlyCollection<Order> FindAll();
    Order? FindById(string id);
    Order Create(CreateOrderDto dto);
    Order UpdateStatus(string id, OrderStatus newStatus);
}
