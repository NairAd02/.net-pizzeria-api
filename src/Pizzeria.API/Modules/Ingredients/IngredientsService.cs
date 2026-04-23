using Microsoft.EntityFrameworkCore;
using Pizzeria.API.Infrastructure.Database;
using Pizzeria.API.Infrastructure.Storage;
using Pizzeria.API.Modules.Ingredients.Dtos;
using Pizzeria.API.Modules.Ingredients.Entities;

namespace Pizzeria.API.Modules.Ingredients;

public class IngredientsService(
    PizzeriaDbContext context,
    IStorageService storage,
    StorageOptions storageOptions) : IIngredientsService
{
    public async Task<IReadOnlyCollection<Ingredient>> FindAllAsync(CancellationToken ct = default)
    {
        var ingredients = await context.Ingredients
            .AsNoTracking()
            .OrderBy(i => i.Code)
            .ToListAsync(ct);
        return ingredients.AsReadOnly();
    }

    public Task<Ingredient?> FindByCodeAsync(string code, CancellationToken ct = default) =>
        context.Ingredients
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Code == code, ct);

    public async Task<Ingredient> CreateAsync(CreateIngredientDto dto, CancellationToken ct = default)
    {
        var exists = await context.Ingredients.AnyAsync(i => i.Code == dto.Code, ct);
        if (exists)
        {
            throw new InvalidOperationException($"Ingredient with code '{dto.Code}' already exists.");
        }

        var ingredient = new Ingredient
        {
            Code = dto.Code,
            Name = dto.Name,
            Stock = dto.Stock,
            PricePerUnit = dto.PricePerUnit,
            Supplier = dto.Supplier,
        };

        context.Ingredients.Add(ingredient);
        await context.SaveChangesAsync(ct);
        return ingredient;
    }

    public async Task<Ingredient> AddStockAsync(string code, decimal quantity, CancellationToken ct = default)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity to add must be positive.", nameof(quantity));
        }

        var ingredient = await context.Ingredients.FirstOrDefaultAsync(i => i.Code == code, ct)
            ?? throw new KeyNotFoundException($"Ingredient '{code}' not found.");

        ingredient.Stock += quantity;
        await context.SaveChangesAsync(ct);
        return ingredient;
    }

    public async Task<Ingredient> AddImageAsync(string code, IFormFile file, string? altText, CancellationToken ct = default)
    {
        ImageFileValidator.EnsureValid(file, storageOptions);

        var ingredient = await context.Ingredients.FirstOrDefaultAsync(i => i.Code == code, ct)
            ?? throw new KeyNotFoundException($"Ingredient '{code}' not found.");

        await using var stream = file.OpenReadStream();
        var image = await storage.UploadAsync(
            content: stream,
            objectKeyPrefix: $"ingredients/{code}",
            originalFileName: file.FileName,
            contentType: file.ContentType,
            size: file.Length,
            altText: altText,
            ct: ct);

        // Reemplazamos la lista (en vez de .Add) para que el ValueComparer detecte el cambio sin dudas.
        ingredient.Images = [.. ingredient.Images, image];
        await context.SaveChangesAsync(ct);
        return ingredient;
    }

    public async Task<Ingredient> RemoveImageAsync(string code, string objectKey, CancellationToken ct = default)
    {
        var ingredient = await context.Ingredients.FirstOrDefaultAsync(i => i.Code == code, ct)
            ?? throw new KeyNotFoundException($"Ingredient '{code}' not found.");

        var image = ingredient.Images.FirstOrDefault(i => i.Key == objectKey)
            ?? throw new KeyNotFoundException($"Image '{objectKey}' not found on ingredient '{code}'.");

        await storage.DeleteAsync(image.Key, ct);

        ingredient.Images = ingredient.Images.Where(i => i.Key != objectKey).ToList();
        await context.SaveChangesAsync(ct);
        return ingredient;
    }
}
