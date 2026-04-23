namespace Pizzeria.API.Modules.DeliveryPersons;

public static class DeliveryPersonsModule
{
    public static IServiceCollection AddDeliveryPersonsModule(this IServiceCollection services)
    {
        services.AddScoped<IDeliveryPersonsService, DeliveryPersonsService>();
        return services;
    }
}
