using Microsoft.EntityFrameworkCore;
using Pizzeria.API.Infrastructure.Database;
using Pizzeria.API.Infrastructure.Storage;
using Pizzeria.API.Modules.Pizzas.Dtos;
using Pizzeria.API.Modules.Pizzas.Entities;

namespace Pizzeria.API.Modules.Pizzas;

public class PizzasService(
    PizzeriaDbContext context,
    IStorageService storage,
    StorageOptions storageOptions) : IPizzasService
{
    public async Task<IReadOnlyCollection<Pizza>> FindAllAsync(CancellationToken ct = default)
    {
        var pizzas = await context.Pizzas
            .AsNoTracking()
            .Include(p => p.Ingredients)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
        return pizzas.AsReadOnly();
    }

    public Task<Pizza?> FindByIdAsync(string id, CancellationToken ct = default) =>
        context.Pizzas
            .AsNoTracking()
            .Include(p => p.Ingredients)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Pizza> CreateAsync(CreatePizzaDto dto, CancellationToken ct = default)
    {
        if (dto.Ingredients is null || dto.Ingredients.Count == 0)
        {
            throw new ArgumentException("A pizza must declare at least one ingredient.");
        }

        // Validamos en una sola consulta que todos los ingredientes existan.
        var codes = dto.Ingredients.Select(i => i.IngredientCode).Distinct().ToList();
        var existingCodes = await context.Ingredients
            .Where(i => codes.Contains(i.Code))
            .Select(i => i.Code)
            .ToListAsync(ct);

        var missing = codes.Except(existingCodes).ToList();
        if (missing.Count > 0)
        {
            throw new KeyNotFoundException(
                $"Ingredients not found: {string.Join(", ", missing)}.");
        }

        var pizza = new Pizza
        {
            Id = Guid.NewGuid().ToString(),
            Name = dto.Name,
            BasePrice = dto.BasePrice,
            Ingredients = dto.Ingredients
                .Select(i => new PizzaIngredient
                {
                    IngredientCode = i.IngredientCode,
                    Quantity = i.Quantity,
                })
                .ToList(),
        };

        context.Pizzas.Add(pizza);
        await context.SaveChangesAsync(ct);
        return pizza;
    }

    public async Task<PizzaCostDto> CalculateCostAsync(string id, CancellationToken ct = default)
    {
        var pizza = await context.Pizzas
            .AsNoTracking()
            .Include(p => p.Ingredients)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new KeyNotFoundException($"Pizza '{id}' not found.");

        var codes = pizza.Ingredients.Select(pi => pi.IngredientCode).ToList();
        var prices = await context.Ingredients
            .Where(i => codes.Contains(i.Code))
            .ToDictionaryAsync(i => i.Code, i => i.PricePerUnit, ct);

        decimal ingredientsCost = 0m;
        foreach (var item in pizza.Ingredients)
        {
            if (!prices.TryGetValue(item.IngredientCode, out var pricePerUnit))
            {
                throw new InvalidOperationException(
                    $"Ingredient '{item.IngredientCode}' no longer exists.");
            }
            ingredientsCost += pricePerUnit * item.Quantity;
        }

        return new PizzaCostDto(
            pizza.Id,
            pizza.Name,
            pizza.BasePrice,
            ingredientsCost,
            pizza.BasePrice + ingredientsCost);
    }

    public async Task<Pizza> AddImageAsync(string id, IFormFile file, string? altText, CancellationToken ct = default)
    {
        ImageFileValidator.EnsureValid(file, storageOptions);

        var pizza = await context.Pizzas
            .Include(p => p.Ingredients)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new KeyNotFoundException($"Pizza '{id}' not found.");

        await using var stream = file.OpenReadStream();
        var image = await storage.UploadAsync(
            content: stream,
            objectKeyPrefix: $"pizzas/{id}",
            originalFileName: file.FileName,
            contentType: file.ContentType,
            size: file.Length,
            altText: altText,
            ct: ct);

        // Reemplazamos la lista (en vez de .Add) para que el ValueComparer detecte el cambio sin dudas.
        pizza.Images = [.. pizza.Images, image];
        await context.SaveChangesAsync(ct);
        return pizza;
    }

    public async Task<Pizza> RemoveImageAsync(string id, string objectKey, CancellationToken ct = default)
    {
        var pizza = await context.Pizzas
            .Include(p => p.Ingredients)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new KeyNotFoundException($"Pizza '{id}' not found.");

        var image = pizza.Images.FirstOrDefault(i => i.Key == objectKey)
            ?? throw new KeyNotFoundException($"Image '{objectKey}' not found on pizza '{id}'.");

        await storage.DeleteAsync(image.Key, ct);

        pizza.Images = pizza.Images.Where(i => i.Key != objectKey).ToList();
        await context.SaveChangesAsync(ct);
        return pizza;
    }
}
