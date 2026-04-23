using Amazon.S3;
using Amazon.S3.Model;

namespace Pizzeria.API.Infrastructure.Storage;

/// <summary>
/// Implementación de <see cref="IStorageService"/> sobre el SDK oficial de AWS
/// (<c>AWSSDK.S3</c>), configurada vía <c>ServiceURL</c> + <c>ForcePathStyle</c>
/// para hablar con cualquier backend S3-compatible.
/// </summary>
public class S3StorageService(IAmazonS3 s3, StorageOptions options) : IStorageService
{
    public async Task<ProductImage> UploadAsync(
        Stream content,
        string objectKeyPrefix,
        string originalFileName,
        string contentType,
        long size,
        string? altText = null,
        CancellationToken ct = default)
    {
        var key = BuildObjectKey(objectKeyPrefix, originalFileName, contentType);

        var request = new PutObjectRequest
        {
            BucketName = options.Bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
        };

        await s3.PutObjectAsync(request, ct);

        return new ProductImage(
            Key: key,
            Url: BuildPublicUrl(key),
            ContentType: contentType,
            Size: size,
            Width: null,
            Height: null,
            AltText: altText,
            CreatedAt: DateTime.UtcNow);
    }

    public async Task DeleteAsync(string objectKey, CancellationToken ct = default)
    {
        await s3.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = options.Bucket,
            Key = objectKey,
        }, ct);
    }

    public string BuildPublicUrl(string objectKey)
    {
        if (!string.IsNullOrWhiteSpace(options.PublicBaseUrl))
        {
            return $"{options.PublicBaseUrl.TrimEnd('/')}/{objectKey}";
        }

        var endpoint = options.Endpoint.TrimEnd('/');
        return $"{endpoint}/{options.Bucket}/{objectKey}";
    }

    private static string BuildObjectKey(string prefix, string originalFileName, string contentType)
    {
        var ext = Path.GetExtension(originalFileName);
        if (string.IsNullOrEmpty(ext))
        {
            ext = contentType.ToLowerInvariant() switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                "image/avif" => ".avif",
                _ => "",
            };
        }

        var cleanPrefix = prefix.Trim('/').ToLowerInvariant();
        return $"{cleanPrefix}/{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
    }
}
