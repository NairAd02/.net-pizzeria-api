namespace Pizzeria.API.Modules.Ingredients;

/// <summary>
/// Equivalente al `@Module` de Nest: aquí se declaran los providers del módulo
/// y, al exponer la extensión, se "importa" desde el AppModule (Program.cs).
/// </summary>
public static class IngredientsModule
{
    public static IServiceCollection AddIngredientsModule(this IServiceCollection services)
    {
        // Scoped porque depende de PizzeriaDbContext, que es scoped por request.
        services.AddScoped<IIngredientsService, IngredientsService>();
        return services;
    }
}
