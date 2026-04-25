namespace Pizzeria.API.Modules.Auth.Jwt;

/// <summary>
/// POCO con la configuración JWT que se rellena desde <c>.env</c>
/// (variables <c>JWT_*</c>). Se registra como singleton en <c>AuthModule</c>.
/// </summary>
public class JwtOptions
{
    public required string Secret { get; init; }
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required int AccessTokenMinutes { get; init; }
    public required int RefreshTokenDays { get; init; }
}
