namespace Pizzeria.API.Infrastructure.Storage;

/// <summary>
/// Contrato agnóstico del proveedor. Los módulos de dominio (Pizzas, Ingredients...)
/// solo conocen esta interfaz: da igual si detrás hay Supabase, AWS S3, R2 o MinIO.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Sube un archivo al bucket y devuelve la metadata lista para persistir
    /// dentro del <c>jsonb</c> del producto.
    /// </summary>
    /// <param name="content">Stream del archivo (el caller es responsable de cerrarlo).</param>
    /// <param name="objectKeyPrefix">Carpeta lógica dentro del bucket, p. ej. <c>pizzas/{id}</c>.</param>
    /// <param name="originalFileName">Nombre original; se usa solo para extraer la extensión.</param>
    /// <param name="contentType">MIME del archivo (ya validado por el caller).</param>
    /// <param name="size">Tamaño en bytes.</param>
    /// <param name="altText">Texto alternativo opcional.</param>
    Task<ProductImage> UploadAsync(
        Stream content,
        string objectKeyPrefix,
        string originalFileName,
        string contentType,
        long size,
        string? altText = null,
        CancellationToken ct = default);

    /// <summary>Borra un objeto del bucket por su <paramref name="objectKey"/>.</summary>
    Task DeleteAsync(string objectKey, CancellationToken ct = default);

    /// <summary>Construye la URL pública final para un <paramref name="objectKey"/>.</summary>
    string BuildPublicUrl(string objectKey);
}
