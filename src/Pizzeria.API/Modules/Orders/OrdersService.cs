using Microsoft.EntityFrameworkCore;
using Pizzeria.API.Infrastructure.Database;
using Pizzeria.API.Modules.DeliveryPersons.Entities;
using Pizzeria.API.Modules.Orders.Dtos;
using Pizzeria.API.Modules.Orders.Entities;

namespace Pizzeria.API.Modules.Orders;

public class OrdersService(PizzeriaDbContext context) : IOrdersService
{
    public async Task<IReadOnlyCollection<Order>> FindAllAsync(CancellationToken ct = default)
    {
        var orders = await context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);
        return orders.AsReadOnly();
    }

    public Task<Order?> FindByIdAsync(string id, CancellationToken ct = default) =>
        context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<Order> CreateAsync(CreateOrderDto dto, CancellationToken ct = default)
    {
        if (dto.Items is null || dto.Items.Count == 0)
        {
            throw new ArgumentException("Order must contain at least one pizza.");
        }

        foreach (var item in dto.Items)
        {
            if (item.Quantity <= 0)
            {
                throw new ArgumentException($"Quantity for pizza '{item.PizzaId}' must be positive.");
            }
        }

        // Toda la operación (verificar stock + descontar + insertar pedido) debe ser
        // atómica: usamos una transacción explícita para que si algo falla, nada persista.
        await using var transaction = await context.Database.BeginTransactionAsync(ct);

        try
        {
            // 1. Cargamos las pizzas involucradas (con sus ingredientes) en una sola consulta.
            var pizzaIds = dto.Items.Select(i => i.PizzaId).Distinct().ToList();
            var pizzas = await context.Pizzas
                .Include(p => p.Ingredients)
                .Where(p => pizzaIds.Contains(p.Id))
                .ToListAsync(ct);

            var missingPizzas = pizzaIds.Except(pizzas.Select(p => p.Id)).ToList();
            if (missingPizzas.Count > 0)
            {
                throw new KeyNotFoundException(
                    $"Pizzas not found: {string.Join(", ", missingPizzas)}.");
            }

            var pizzaById = pizzas.ToDictionary(p => p.Id);

            // 2. Agregamos los ingredientes requeridos (cantidad por pizza × número de pizzas del pedido).
            var required = new Dictionary<string, decimal>();
            foreach (var item in dto.Items)
            {
                var pizza = pizzaById[item.PizzaId];
                foreach (var pi in pizza.Ingredients)
                {
                    var totalNeeded = pi.Quantity * item.Quantity;
                    required[pi.IngredientCode] =
                        required.GetValueOrDefault(pi.IngredientCode) + totalNeeded;
                }
            }

            // 3. Cargamos los ingredientes necesarios ya trackeados para poder modificar su stock.
            var neededCodes = required.Keys.ToList();
            var ingredients = await context.Ingredients
                .Where(i => neededCodes.Contains(i.Code))
                .ToListAsync(ct);

            var ingredientByCode = ingredients.ToDictionary(i => i.Code);

            // 4. Validamos que haya stock suficiente de TODOS los ingredientes antes de descontar ninguno.
            foreach (var (code, quantity) in required)
            {
                if (!ingredientByCode.TryGetValue(code, out var ingredient))
                {
                    throw new InvalidOperationException(
                        $"Ingredient '{code}' not found in stock.");
                }

                if (ingredient.Stock < quantity)
                {
                    throw new InvalidOperationException(
                        $"Not enough stock for ingredient '{code}'. " +
                        $"Available: {ingredient.Stock}, required: {quantity}.");
                }
            }

            // 5. Descontamos el stock (los objetos están trackeados, EF detecta los cambios).
            foreach (var (code, quantity) in required)
            {
                ingredientByCode[code].Stock -= quantity;
            }

            // 6. Creamos el pedido.
            var order = new Order
            {
                Id = Guid.NewGuid().ToString(),
                CustomerName = dto.CustomerName,
                CustomerPhone = dto.CustomerPhone,
                Items = dto.Items
                    .Select(i => new OrderItem { PizzaId = i.PizzaId, Quantity = i.Quantity })
                    .ToList(),
            };

            context.Orders.Add(order);

            // 7. Guardamos todo en una sola llamada y commiteamos.
            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return order;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<Order> UpdateStatusAsync(string id, OrderStatus newStatus, CancellationToken ct = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(ct);

        try
        {
            var order = await context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id, ct)
                ?? throw new KeyNotFoundException($"Order '{id}' not found.");

            switch (newStatus)
            {
                case OrderStatus.OnTheWay:
                    await AssignAvailableDeliveryPersonAsync(order, ct);
                    break;

                case OrderStatus.Delivered:
                    await ReleaseDeliveryPersonAsync(order, ct);
                    break;
            }

            order.Status = newStatus;

            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return order;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private async Task AssignAvailableDeliveryPersonAsync(Order order, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(order.AssignedDeliveryPersonCode))
        {
            return; // Ya tiene uno asignado; no lo duplicamos.
        }

        var available = await context.DeliveryPersons
            .FirstOrDefaultAsync(p => p.Status == DeliveryPersonStatus.Available, ct)
            ?? throw new InvalidOperationException("No delivery persons available right now.");

        available.Status = DeliveryPersonStatus.Busy;
        order.AssignedDeliveryPersonCode = available.Code;
    }

    private async Task ReleaseDeliveryPersonAsync(Order order, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(order.AssignedDeliveryPersonCode))
        {
            return;
        }

        var person = await context.DeliveryPersons
            .FirstOrDefaultAsync(p => p.Code == order.AssignedDeliveryPersonCode, ct);

        if (person is not null)
        {
            person.Status = DeliveryPersonStatus.Available;
        }
    }
}
