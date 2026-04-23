namespace Pizzeria.API.Modules.Pizzas;

/// <summary>
/// Este módulo depende de `IngredientsModule` (el servicio se inyecta por DI).
/// En Nest sería el equivalente a poner `IngredientsModule` dentro del array `imports`.
/// </summary>
public static class PizzasModule
{
    public static IServiceCollection AddPizzasModule(this IServiceCollection services)
    {
        services.AddSingleton<IPizzasService, PizzasService>();
        return services;
    }
}
