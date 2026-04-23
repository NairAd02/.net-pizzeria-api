using System.Collections.Concurrent;
using Pizzeria.API.Modules.DeliveryPersons;
using Pizzeria.API.Modules.Ingredients;
using Pizzeria.API.Modules.Orders.Dtos;
using Pizzeria.API.Modules.Orders.Entities;
using Pizzeria.API.Modules.Pizzas;

namespace Pizzeria.API.Modules.Orders;

public class OrdersService(
    IPizzasService pizzasService,
    IIngredientsService ingredientsService,
    IDeliveryPersonsService deliveryPersonsService) : IOrdersService
{
    private readonly ConcurrentDictionary<string, Order> _store = new();

    public IReadOnlyCollection<Order> FindAll() => _store.Values.ToList().AsReadOnly();

    public Order? FindById(string id) =>
        _store.TryGetValue(id, out var order) ? order : null;

    public Order Create(CreateOrderDto dto)
    {
        if (dto.Items is null || dto.Items.Count == 0)
        {
            throw new ArgumentException("Order must contain at least one pizza.");
        }

        // 1. Agregamos las cantidades requeridas de cada ingrediente sumando todas las pizzas del pedido.
        var required = new Dictionary<string, decimal>();
        foreach (var item in dto.Items)
        {
            if (item.Quantity <= 0)
            {
                throw new ArgumentException($"Quantity for pizza '{item.PizzaId}' must be positive.");
            }

            var pizza = pizzasService.FindById(item.PizzaId)
                ?? throw new KeyNotFoundException($"Pizza '{item.PizzaId}' not found.");

            foreach (var ingredient in pizza.Ingredients)
            {
                var totalNeeded = ingredient.Quantity * item.Quantity;
                required[ingredient.IngredientCode] =
                    required.GetValueOrDefault(ingredient.IngredientCode) + totalNeeded;
            }
        }

        // 2. Verificamos stock de TODOS los ingredientes antes de tocar nada.
        foreach (var (code, quantity) in required)
        {
            if (!ingredientsService.HasStock(code, quantity))
            {
                throw new InvalidOperationException(
                    $"Not enough stock for ingredient '{code}'. Required: {quantity}.");
            }
        }

        // 3. Solo si todo pasa, descontamos el stock.
        foreach (var (code, quantity) in required)
        {
            ingredientsService.DecrementStock(code, quantity);
        }

        var order = new Order
        {
            Id = Guid.NewGuid().ToString(),
            CustomerName = dto.CustomerName,
            CustomerPhone = dto.CustomerPhone,
            Items = dto.Items
                .Select(i => new OrderItem { PizzaId = i.PizzaId, Quantity = i.Quantity })
                .ToList(),
        };

        _store[order.Id] = order;
        return order;
    }

    public Order UpdateStatus(string id, OrderStatus newStatus)
    {
        var order = FindById(id)
            ?? throw new KeyNotFoundException($"Order '{id}' not found.");

        switch (newStatus)
        {
            case OrderStatus.OnTheWay:
                AssignAvailableDeliveryPerson(order);
                break;

            case OrderStatus.Delivered:
                ReleaseDeliveryPerson(order);
                break;
        }

        order.Status = newStatus;
        return order;
    }

    private void AssignAvailableDeliveryPerson(Order order)
    {
        if (!string.IsNullOrEmpty(order.AssignedDeliveryPersonCode))
        {
            return; // Ya tiene uno asignado, no lo duplicamos.
        }

        var available = deliveryPersonsService.FindAvailable()
            ?? throw new InvalidOperationException("No delivery persons available right now.");

        deliveryPersonsService.MarkBusy(available.Code);
        order.AssignedDeliveryPersonCode = available.Code;
    }

    private void ReleaseDeliveryPerson(Order order)
    {
        if (string.IsNullOrEmpty(order.AssignedDeliveryPersonCode))
        {
            return;
        }

        deliveryPersonsService.MarkAvailable(order.AssignedDeliveryPersonCode);
    }
}
