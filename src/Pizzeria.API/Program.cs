using System.Text.Json.Serialization;
using DotNetEnv;
using Pizzeria.API.Infrastructure.Database;
using Pizzeria.API.Modules.DeliveryPersons;
using Pizzeria.API.Modules.Ingredients;
using Pizzeria.API.Modules.Orders;
using Pizzeria.API.Modules.Pizzas;

// Cargamos .env lo más temprano posible para que las variables también estén
// disponibles en design-time (p. ej. al correr `dotnet ef migrations add`).
Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Infra global (equivalente a lo que Nest configura en main.ts + AppModule).
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Serializa los enums como string ("Pending", "OnTheWay"...), igual que Nest por defecto.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddOpenApi();

// Registro de módulos de la aplicación (equivalente a los `imports` del AppModule de Nest).
builder.Services
    .AddDatabaseModule()
    .AddIngredientsModule()
    .AddPizzasModule()
    .AddDeliveryPersonsModule()
    .AddOrdersModule();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
