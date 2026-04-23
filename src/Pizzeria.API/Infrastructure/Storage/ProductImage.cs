namespace Pizzeria.API.Infrastructure.Storage;

/// <summary>
/// Tipo compartido que representa una imagen asociada a un producto (pizza, ingrediente...).
/// Se persiste como un elemento dentro de una columna <c>jsonb</c>.
/// Al estar fuertemente tipado podemos añadir metadatos sin cambiar el esquema.
/// </summary>
public record ProductImage(
    string Key,
    string Url,
    string ContentType,
    long Size,
    int? Width,
    int? Height,
    string? AltText,
    DateTime CreatedAt);
