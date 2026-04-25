using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pizzeria.API.Infrastructure.Database;
using Pizzeria.API.Modules.Auth.Dtos;
using Pizzeria.API.Modules.Auth.Entities;
using Pizzeria.API.Modules.Auth.Jwt;

namespace Pizzeria.API.Modules.Auth;

public class AuthService(
    PizzeriaDbContext context,
    IJwtTokenService jwtTokenService,
    IPasswordHasher<User> passwordHasher) : IAuthService
{
    public async Task<AuthResponseDto> SignUpAsync(SignUpDto dto, CancellationToken ct = default)
    {
        var email = NormalizeEmail(dto.Email);

        var emailTaken = await context.Users.AnyAsync(u => u.Email == email, ct);
        if (emailTaken)
        {
            throw new InvalidOperationException($"Email '{email}' is already registered.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = string.Empty, // se rellena justo debajo; PasswordHasher necesita la entidad.
            Role = UserRole.Client,      // sign-up público siempre crea Client.
        };
        user.PasswordHash = passwordHasher.HashPassword(user, dto.Password);

        context.Users.Add(user);
        await context.SaveChangesAsync(ct);

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResponseDto> SignInAsync(SignInDto dto, CancellationToken ct = default)
    {
        var email = NormalizeEmail(dto.Email);
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null)
        {
            // Mismo mensaje genérico para email inexistente y password incorrecto,
            // para no revelar qué emails están registrados.
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, dto.Password);
        }

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResponseDto> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var tokenHash = jwtTokenService.HashRefreshToken(refreshToken);
        var stored = await context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (stored.RevokedAt is not null)
        {
            // Reuso de un refresh ya revocado ⇒ posible robo. Revocamos TODOS
            // los refresh tokens activos del usuario como precaución.
            await RevokeAllActiveTokensForUserAsync(stored.UserId, ct);
            await context.SaveChangesAsync(ct);
            throw new UnauthorizedAccessException("Refresh token has been revoked.");
        }

        if (DateTime.UtcNow >= stored.ExpiresAt)
        {
            throw new UnauthorizedAccessException("Refresh token has expired.");
        }

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == stored.UserId, ct)
            ?? throw new UnauthorizedAccessException("User no longer exists.");

        // Rotación: revocamos el viejo y emitimos uno nuevo, enlazándolos.
        var (accessToken, accessExpiresAt) = jwtTokenService.GenerateAccessToken(user);
        var (newRefresh, newHash, newExpiresAt) = jwtTokenService.GenerateRefreshToken();

        var newStored = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = newHash,
            ExpiresAt = newExpiresAt,
        };
        context.RefreshTokens.Add(newStored);

        stored.RevokedAt = DateTime.UtcNow;
        stored.ReplacedByTokenId = newStored.Id;

        await context.SaveChangesAsync(ct);

        return new AuthResponseDto(
            accessToken,
            newRefresh,
            accessExpiresAt,
            new AuthUserDto(user.Id, user.Email, user.Role));
    }

    public async Task SignOutAsync(string refreshToken, CancellationToken ct = default)
    {
        var tokenHash = jwtTokenService.HashRefreshToken(refreshToken);
        var stored = await context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

        // Sign-out es idempotente: si el token no existe o ya estaba revocado,
        // devolvemos OK sin error (no queremos filtrar información).
        if (stored is null || stored.RevokedAt is not null)
        {
            return;
        }

        stored.RevokedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
    }

    public async Task<MeDto?> GetMeAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        return user is null
            ? null
            : new MeDto(user.Id, user.Email, user.Role, user.CreatedAt);
    }

    private async Task<AuthResponseDto> IssueTokensAsync(User user, CancellationToken ct)
    {
        var (accessToken, accessExpiresAt) = jwtTokenService.GenerateAccessToken(user);
        var (refreshToken, refreshHash, refreshExpiresAt) = jwtTokenService.GenerateRefreshToken();

        context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAt = refreshExpiresAt,
        });

        await context.SaveChangesAsync(ct);

        return new AuthResponseDto(
            accessToken,
            refreshToken,
            accessExpiresAt,
            new AuthUserDto(user.Id, user.Email, user.Role));
    }

    private async Task RevokeAllActiveTokensForUserAsync(Guid userId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var active = await context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
            .ToListAsync(ct);

        foreach (var token in active)
        {
            token.RevokedAt = now;
        }
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
