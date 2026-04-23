using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Pizzeria.API.Infrastructure.Storage;
using Pizzeria.API.Modules.DeliveryPersons.Entities;
using Pizzeria.API.Modules.Ingredients.Entities;
using Pizzeria.API.Modules.Orders.Entities;
using Pizzeria.API.Modules.Pizzas.Entities;

namespace Pizzeria.API.Infrastructure.Database;

/// <summary>
/// DbContext único compartido por todos los módulos (equivalente al
/// <c>DataSource</c> compartido en Nest + TypeORM). Cada módulo sigue siendo
/// dueño de sus entities; aquí solo las registramos para que EF Core las
/// conozca y mapee a PostgreSQL.
/// </summary>
public class PizzeriaDbContext(DbContextOptions<PizzeriaDbContext> options) : DbContext(options)
{
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<Pizza> Pizzas => Set<Pizza>();
    public DbSet<PizzaIngredient> PizzaIngredients => Set<PizzaIngredient>();
    public DbSet<DeliveryPerson> DeliveryPersons => Set<DeliveryPerson>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    // Serializador compartido para las columnas jsonb de ProductImage[].
    private static readonly JsonSerializerOptions ImagesJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly ValueConverter<List<ProductImage>, string> ProductImagesConverter = new(
        v => JsonSerializer.Serialize(v, ImagesJsonOptions),
        v => string.IsNullOrWhiteSpace(v)
            ? new List<ProductImage>()
            : JsonSerializer.Deserialize<List<ProductImage>>(v, ImagesJsonOptions) ?? new List<ProductImage>());

    // EF necesita un comparer explícito para detectar cambios dentro de una lista
    // que se persiste como una sola columna (si no, añadir/quitar una imagen no se persiste).
    private static readonly ValueComparer<List<ProductImage>> ProductImagesComparer = new(
        (a, b) => JsonSerializer.Serialize(a, ImagesJsonOptions) == JsonSerializer.Serialize(b, ImagesJsonOptions),
        v => v == null ? 0 : JsonSerializer.Serialize(v, ImagesJsonOptions).GetHashCode(),
        v => JsonSerializer.Deserialize<List<ProductImage>>(
                JsonSerializer.Serialize(v, ImagesJsonOptions), ImagesJsonOptions) ?? new List<ProductImage>());

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureIngredients(modelBuilder);
        ConfigurePizzas(modelBuilder);
        ConfigureDeliveryPersons(modelBuilder);
        ConfigureOrders(modelBuilder);
    }

    private static void ConfigureIngredients(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ingredient>(b =>
        {
            b.ToTable("ingredients");
            b.HasKey(i => i.Code);
            b.Property(i => i.Code).HasMaxLength(50);
            b.Property(i => i.Name).HasMaxLength(200).IsRequired();
            b.Property(i => i.Stock).HasColumnType("numeric(12, 3)");
            b.Property(i => i.PricePerUnit).HasColumnType("numeric(12, 4)");
            b.Property(i => i.Supplier).HasMaxLength(200);

            b.Property(i => i.Images)
                .HasColumnName("images")
                .HasColumnType("jsonb")
                .HasConversion(ProductImagesConverter)
                .Metadata.SetValueComparer(ProductImagesComparer);
        });
    }

    private static void ConfigurePizzas(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Pizza>(b =>
        {
            b.ToTable("pizzas");
            b.HasKey(p => p.Id);
            b.Property(p => p.Id).HasMaxLength(64);
            b.Property(p => p.Name).HasMaxLength(200).IsRequired();
            b.Property(p => p.BasePrice).HasColumnType("numeric(12, 2)");

            b.Property(p => p.Images)
                .HasColumnName("images")
                .HasColumnType("jsonb")
                .HasConversion(ProductImagesConverter)
                .Metadata.SetValueComparer(ProductImagesComparer);

            b.HasMany(p => p.Ingredients)
                .WithOne()
                .HasForeignKey(pi => pi.PizzaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PizzaIngredient>(b =>
        {
            b.ToTable("pizza_ingredients");
            b.HasKey(pi => pi.Id);
            b.Property(pi => pi.PizzaId).HasMaxLength(64);
            b.Property(pi => pi.IngredientCode).HasMaxLength(50);
            b.Property(pi => pi.Quantity).HasColumnType("numeric(12, 3)");

            // Un ingrediente no puede borrarse si está usado en alguna receta.
            b.HasOne<Ingredient>()
                .WithMany()
                .HasForeignKey(pi => pi.IngredientCode)
                .HasPrincipalKey(i => i.Code)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(pi => new { pi.PizzaId, pi.IngredientCode }).IsUnique();
        });
    }

    private static void ConfigureDeliveryPersons(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DeliveryPerson>(b =>
        {
            b.ToTable("delivery_persons");
            b.HasKey(d => d.Code);
            b.Property(d => d.Code).HasMaxLength(50);
            b.Property(d => d.Name).HasMaxLength(200).IsRequired();
            b.Property(d => d.Phone).HasMaxLength(50).IsRequired();
            b.Property(d => d.Status)
                .HasConversion<string>()
                .HasMaxLength(20);
        });
    }

    private static void ConfigureOrders(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(b =>
        {
            b.ToTable("orders");
            b.HasKey(o => o.Id);
            b.Property(o => o.Id).HasMaxLength(64);
            b.Property(o => o.CustomerName).HasMaxLength(200).IsRequired();
            b.Property(o => o.CustomerPhone).HasMaxLength(50).IsRequired();
            b.Property(o => o.CreatedAt);
            b.Property(o => o.Status)
                .HasConversion<string>()
                .HasMaxLength(20);
            b.Property(o => o.AssignedDeliveryPersonCode).HasMaxLength(50);

            b.HasMany(o => o.Items)
                .WithOne()
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Un pedido puede tener asignado un repartidor (opcional) y no lo borramos si lo tiene.
            b.HasOne<DeliveryPerson>()
                .WithMany()
                .HasForeignKey(o => o.AssignedDeliveryPersonCode)
                .HasPrincipalKey(d => d.Code)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderItem>(b =>
        {
            b.ToTable("order_items");
            b.HasKey(i => i.Id);
            b.Property(i => i.OrderId).HasMaxLength(64);
            b.Property(i => i.PizzaId).HasMaxLength(64);
            b.Property(i => i.Quantity);

            // No borrar una pizza si hay pedidos históricos que la referencian.
            b.HasOne<Pizza>()
                .WithMany()
                .HasForeignKey(i => i.PizzaId)
                .HasPrincipalKey(p => p.Id)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
