using Amazon.Runtime;
using Amazon.S3;

namespace Pizzeria.API.Infrastructure.Storage;

/// <summary>
/// Módulo de infraestructura: configura el cliente S3 leyendo las variables
/// <c>STORAGE_*</c> desde el <c>.env</c>. Pensado para ser provider-agnostic:
/// cambiar de Supabase a AWS S3, Cloudflare R2 o MinIO es solo editar el <c>.env</c>.
/// </summary>
public static class StorageModule
{
    public static IServiceCollection AddStorageModule(this IServiceCollection services)
    {
        var options = LoadFromEnv();
        services.AddSingleton(options);

        services.AddSingleton<IAmazonS3>(_ =>
        {
            var credentials = new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey);
            var config = new AmazonS3Config
            {
                ServiceURL = options.Endpoint,
                ForcePathStyle = options.ForcePathStyle,
                AuthenticationRegion = options.Region,
            };
            return new AmazonS3Client(credentials, config);
        });

        services.AddScoped<IStorageService, S3StorageService>();

        return services;
    }

    private static StorageOptions LoadFromEnv()
    {
        var opts = new StorageOptions
        {
            Provider = GetOptional("STORAGE_PROVIDER") ?? "supabase",
            Endpoint = GetRequired("STORAGE_ENDPOINT"),
            Region = GetOptional("STORAGE_REGION") ?? "us-east-1",
            AccessKeyId = GetRequired("STORAGE_ACCESS_KEY_ID"),
            SecretAccessKey = GetRequired("STORAGE_SECRET_ACCESS_KEY"),
            Bucket = GetRequired("STORAGE_BUCKET"),
            PublicBaseUrl = GetOptional("STORAGE_PUBLIC_BASE_URL"),
            ForcePathStyle = GetBool("STORAGE_FORCE_PATH_STYLE", defaultValue: true),
            MaxFileSizeBytes = GetLong("STORAGE_MAX_FILE_SIZE_MB", defaultValue: 5) * 1024 * 1024,
        };

        var allowed = GetOptional("STORAGE_ALLOWED_CONTENT_TYPES");
        if (!string.IsNullOrWhiteSpace(allowed))
        {
            opts.AllowedContentTypes = allowed
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        return opts;
    }

    private static string GetRequired(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Missing required environment variable '{key}'. " +
                "Make sure your .env file is present and all STORAGE_* variables are set.");
        }
        return value;
    }

    private static string? GetOptional(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static bool GetBool(string key, bool defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value) ? defaultValue : bool.Parse(value);
    }

    private static long GetLong(string key, long defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value) ? defaultValue : long.Parse(value);
    }
}
