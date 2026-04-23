using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Pizzeria.API.Infrastructure.Database;

/// <summary>
/// Factory de <see cref="PizzeriaDbContext"/> usada sólo en design-time
/// (cuando corres <c>dotnet ef migrations add ...</c> o
/// <c>dotnet ef database update</c>). Evita que EF tenga que arrancar el
/// <c>WebApplication</c> completo del proyecto, y permite generar migraciones
/// incluso si todavía no existe el archivo <c>.env</c> en disco.
/// </summary>
public class PizzeriaDbContextFactory : IDesignTimeDbContextFactory<PizzeriaDbContext>
{
    public PizzeriaDbContext CreateDbContext(string[] args)
    {
        // Carga .env si existe; si no, seguimos con placeholders (suficientes
        // para `migrations add`). Para `database update` sí necesitas credenciales reales.
        Env.TraversePath().Load();

        var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
        var database = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "pizzeria";
        var user = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "postgres";
        var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "postgres";

        var connectionString =
            $"Host={host};Port={port};Database={database};Username={user};Password={password}";

        var options = new DbContextOptionsBuilder<PizzeriaDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new PizzeriaDbContext(options);
    }
}
