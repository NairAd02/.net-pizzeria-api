using Pizzeria.API.Modules.Orders.Dtos;
using Pizzeria.API.Modules.Orders.Entities;

namespace Pizzeria.API.Modules.Orders;

public interface IOrdersService
{
    Task<IReadOnlyCollection<Order>> FindAllAsync(CancellationToken ct = default);
    Task<Order?> FindByIdAsync(string id, CancellationToken ct = default);
    Task<Order> CreateAsync(CreateOrderDto dto, CancellationToken ct = default);
    Task<Order> UpdateStatusAsync(string id, OrderStatus newStatus, CancellationToken ct = default);
}
