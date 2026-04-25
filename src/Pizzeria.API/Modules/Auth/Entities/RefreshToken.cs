namespace Pizzeria.API.Modules.Auth.Entities;

/// <summary>
/// Refresh token persistido. Solo se guarda el SHA-256 del token (nunca el
/// token en claro), como defensa en profundidad por si la BD se filtrara.
/// Cada refresh se rota: cuando se usa para pedir un par nuevo, se marca con
/// <see cref="RevokedAt"/> y <see cref="ReplacedByTokenId"/> apunta al nuevo.
/// </summary>
public class RefreshToken
{
    public required Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required string TokenHash { get; set; }
    public required DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
    public Guid? ReplacedByTokenId { get; set; }

    public bool IsActive => RevokedAt is null && DateTime.UtcNow < ExpiresAt;
}
