namespace Pizzeria.API.Infrastructure.Storage;

/// <summary>
/// Configuración del storage S3-compatible. Todos los campos se leen desde
/// variables de entorno (<c>.env</c>) en <see cref="StorageModule"/>.
/// El mismo POCO sirve para Supabase, AWS S3, Cloudflare R2, MinIO, Wasabi, etc.:
/// solo cambia lo que ponemos en el <c>.env</c>.
/// </summary>
public class StorageOptions
{
    /// <summary>Etiqueta informativa, no cambia el comportamiento ("supabase", "aws", "r2", "minio"...).</summary>
    public string Provider { get; set; } = "supabase";

    /// <summary>Endpoint S3. Ej. <c>https://xxxxx.supabase.co/storage/v1/s3</c>.</summary>
    public string Endpoint { get; set; } = "";

    /// <summary>Región. Algunos proveedores la exigen (AWS) y otros la ignoran, pero el SDK la pide.</summary>
    public string Region { get; set; } = "us-east-1";

    public string AccessKeyId { get; set; } = "";

    public string SecretAccessKey { get; set; } = "";

    public string Bucket { get; set; } = "";

    /// <summary>
    /// URL base pública para construir los URLs finales. Si es null, se usa <c>{Endpoint}/{Bucket}</c>.
    /// Supabase requiere su patrón propio: <c>https://xxxxx.supabase.co/storage/v1/object/public/{bucket}</c>.
    /// </summary>
    public string? PublicBaseUrl { get; set; }

    /// <summary>
    /// Casi todos los proveedores no-AWS requieren path-style (<c>host/bucket/key</c>) en vez de virtual-host.
    /// </summary>
    public bool ForcePathStyle { get; set; } = true;

    /// <summary>Tamaño máximo aceptado por archivo (default 5 MB).</summary>
    public long MaxFileSizeBytes { get; set; } = 5L * 1024 * 1024;

    /// <summary>Content-types permitidos (case-insensitive).</summary>
    public HashSet<string> AllowedContentTypes { get; set; } =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp",
            "image/avif",
        };
}
