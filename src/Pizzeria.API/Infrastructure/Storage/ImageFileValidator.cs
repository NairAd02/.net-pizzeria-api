namespace Pizzeria.API.Infrastructure.Storage;

/// <summary>
/// Helper compartido para validar <see cref="IFormFile"/>s contra las reglas
/// definidas en <see cref="StorageOptions"/>. Lo usan los services de dominio
/// (Pizzas, Ingredients...) antes de llamar a <see cref="IStorageService"/>.
/// </summary>
public static class ImageFileValidator
{
    public static void EnsureValid(IFormFile? file, StorageOptions options)
    {
        if (file is null || file.Length == 0)
        {
            throw new ArgumentException("No file was provided.", nameof(file));
        }

        if (file.Length > options.MaxFileSizeBytes)
        {
            var maxMb = options.MaxFileSizeBytes / 1024d / 1024d;
            throw new ArgumentException(
                $"File exceeds the maximum allowed size of {maxMb:0.##} MB.",
                nameof(file));
        }

        if (string.IsNullOrWhiteSpace(file.ContentType) ||
            !options.AllowedContentTypes.Contains(file.ContentType))
        {
            var allowed = string.Join(", ", options.AllowedContentTypes);
            throw new ArgumentException(
                $"Content type '{file.ContentType}' is not allowed. Allowed types: {allowed}.",
                nameof(file));
        }
    }
}
