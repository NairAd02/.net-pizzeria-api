namespace Pizzeria.API.Modules.Orders;

/// <summary>
/// Este módulo depende de `PizzasModule`, `IngredientsModule` y `DeliveryPersonsModule`.
/// En Nest los pondrías a los tres en el array `imports` de `OrdersModule`.
/// </summary>
public static class OrdersModule
{
    public static IServiceCollection AddOrdersModule(this IServiceCollection services)
    {
        services.AddSingleton<IOrdersService, OrdersService>();
        return services;
    }
}
