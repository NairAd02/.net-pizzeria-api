namespace Pizzeria.API.Modules.Ingredients;

/// <summary>
/// Equivalente al `@Module` de Nest: aquí se declaran los providers del módulo
/// y, al exponer la extensión, se "importa" desde el AppModule (Program.cs).
/// </summary>
public static class IngredientsModule
{
    public static IServiceCollection AddIngredientsModule(this IServiceCollection services)
    {
        services.AddSingleton<IIngredientsService, IngredientsService>();
        return services;
    }
}
