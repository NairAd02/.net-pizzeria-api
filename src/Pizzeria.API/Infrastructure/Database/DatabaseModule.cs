using Microsoft.EntityFrameworkCore;

namespace Pizzeria.API.Infrastructure.Database;

/// <summary>
/// Módulo de infraestructura: configura la conexión a PostgreSQL leyendo las
/// variables de entorno cargadas desde <c>.env</c>.
/// </summary>
public static class DatabaseModule
{
    public static IServiceCollection AddDatabaseModule(this IServiceCollection services)
    {
        var connectionString = BuildConnectionString();

        services.AddDbContext<PizzeriaDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }

    private static string BuildConnectionString()
    {
        var host = GetRequired("POSTGRES_HOST");
        var port = GetRequired("POSTGRES_PORT");
        var database = GetRequired("POSTGRES_DB");
        var user = GetRequired("POSTGRES_USER");
        var password = GetRequired("POSTGRES_PASSWORD");

        return $"Host={host};Port={port};Database={database};Username={user};Password={password}";
    }

    private static string GetRequired(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Missing required environment variable '{key}'. " +
                "Make sure your .env file is present and all POSTGRES_* variables are set.");
        }
        return value;
    }
}
