using Pizzeria.API.Modules.Auth.Entities;

namespace Pizzeria.API.Modules.Auth.Dtos;

/// <summary>
/// Respuesta que devuelven <c>/sign-in</c>, <c>/sign-up</c> y <c>/refresh</c>.
/// </summary>
public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    AuthUserDto User);

public record AuthUserDto(Guid Id, string Email, UserRole Role);
