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

    public async Task<Pizza> AddImagesAsync(
        string id,
        IReadOnlyList<IFormFile> files,
        IReadOnlyList<string?>? altTexts,
        CancellationToken ct = default)
    {
        if (files is null || files.Count == 0)
        {
            throw new ArgumentException("At least one file must be provided.", nameof(files));
        }

        // Validamos todos los archivos ANTES de empezar a subir, así fallamos rápido
        // sin dejar nada a medias en el storage.
        foreach (var file in files)
        {
            ImageFileValidator.EnsureValid(file, storageOptions);
        }

        var pizza = await context.Pizzas
            .Include(p => p.Ingredients)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new KeyNotFoundException($"Pizza '{id}' not found.");

        var uploaded = new List<ProductImage>(files.Count);
        try
        {
            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];
                var altText = altTexts is not null && i < altTexts.Count && !string.IsNullOrWhiteSpace(altTexts[i])
                    ? altTexts[i]
                    : null;

                await using var stream = file.OpenReadStream();
                var image = await storage.UploadAsync(
                    content: stream,
                    objectKeyPrefix: $"pizzas/{id}",
                    originalFileName: file.FileName,
                    contentType: file.ContentType,
                    size: file.Length,
                    altText: altText,
                    ct: ct);
                uploaded.Add(image);
            }
        }
        catch
        {
            // Si falla a mitad, borramos del storage lo ya subido para no dejar archivos huérfanos.
            // Best-effort: ignoramos errores del delete para no enmascarar la excepción original.
            foreach (var orphan in uploaded)
            {
                try { await storage.DeleteAsync(orphan.Key, CancellationToken.None); }
                catch { /* best effort */ }
            }
            throw;
        }

        // Reemplazamos la lista (en vez de .AddRange) para que el ValueComparer detecte el cambio sin dudas.
        pizza.Images = [.. pizza.Images, .. uploaded];
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
