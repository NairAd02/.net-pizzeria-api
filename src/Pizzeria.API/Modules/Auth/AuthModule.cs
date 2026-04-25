using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Pizzeria.API.Infrastructure.Database;
using Pizzeria.API.Modules.Auth.Entities;
using Pizzeria.API.Modules.Auth.Jwt;

namespace Pizzeria.API.Modules.Auth;

/// <summary>
/// Módulo de autenticación:
///  - Carga <see cref="JwtOptions"/> desde <c>.env</c> (variables <c>JWT_*</c>).
///  - Registra <see cref="IAuthService"/>, <see cref="IJwtTokenService"/> y <see cref="IPasswordHasher{User}"/>.
///  - Configura el esquema <c>JwtBearer</c> para que <c>[Authorize]</c> valide el JWT.
///  - Expone <see cref="SeedInitialAdminAsync"/> para crear un admin al arranque si no existe ninguno.
/// </summary>
public static class AuthModule
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services)
    {
        var options = BuildJwtOptions();
        services.AddSingleton(options);

        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

        // Necesario para que los services puedan leer el user id del HttpContext
        // (lo usa OrdersService para filtrar/crear pedidos por cliente).
        services.AddHttpContextAccessor();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = options.Issuer,
                    ValidAudience = options.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Secret)),
                    // 5 min por defecto es demasiado con access tokens de 15 min.
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// Crea el admin inicial (si no hay ningún usuario con rol <see cref="UserRole.Admin"/>)
    /// leyendo <c>INITIAL_ADMIN_EMAIL</c> e <c>INITIAL_ADMIN_PASSWORD</c> del <c>.env</c>.
    /// Es idempotente: en arranques siguientes no hace nada.
    /// </summary>
    public static async Task SeedInitialAdminAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        var email = Environment.GetEnvironmentVariable("INITIAL_ADMIN_EMAIL");
        var password = Environment.GetEnvironmentVariable("INITIAL_ADMIN_PASSWORD");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            // Sin credenciales ⇒ nada que sembrar. No es un error: permite
            // correr los tests / migrations sin exigir estas variables.
            return;
        }

        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<PizzeriaDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        var hasAdmin = await context.Users.AnyAsync(u => u.Role == UserRole.Admin, ct);
        if (hasAdmin)
        {
            return;
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var existing = await context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);
        if (existing is not null)
        {
            // Ya existía como Client; lo promovemos a Admin para no duplicar emails.
            existing.Role = UserRole.Admin;
            await context.SaveChangesAsync(ct);
            return;
        }

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = string.Empty,
            Role = UserRole.Admin,
        };
        admin.PasswordHash = hasher.HashPassword(admin, password);

        context.Users.Add(admin);
        await context.SaveChangesAsync(ct);
    }

    private static JwtOptions BuildJwtOptions()
    {
        var secret = GetRequired("JWT_SECRET");
        if (Encoding.UTF8.GetByteCount(secret) < 32)
        {
            throw new InvalidOperationException(
                "JWT_SECRET must be at least 32 bytes long for HMAC-SHA256. " +
                "Generate one with e.g. `openssl rand -base64 48`.");
        }

        return new JwtOptions
        {
            Secret = secret,
            Issuer = GetRequired("JWT_ISSUER"),
            Audience = GetRequired("JWT_AUDIENCE"),
            AccessTokenMinutes = GetRequiredInt("JWT_ACCESS_EXPIRES_MINUTES"),
            RefreshTokenDays = GetRequiredInt("JWT_REFRESH_EXPIRES_DAYS"),
        };
    }

    private static string GetRequired(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Missing required environment variable '{key}'. " +
                "Make sure your .env file is present and all JWT_* variables are set.");
        }
        return value;
    }

    private static int GetRequiredInt(string key)
    {
        var raw = GetRequired(key);
        if (!int.TryParse(raw, out var value) || value <= 0)
        {
            throw new InvalidOperationException(
                $"Environment variable '{key}' must be a positive integer, got '{raw}'.");
        }
        return value;
    }
}
