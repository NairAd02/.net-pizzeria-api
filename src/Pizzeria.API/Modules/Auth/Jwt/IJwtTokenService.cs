using Pizzeria.API.Modules.Auth.Entities;

namespace Pizzeria.API.Modules.Auth.Jwt;

/// <summary>
/// Servicio que genera access tokens (JWT firmado) y refresh tokens (string
/// aleatorio opaco). Es stateless — no habla con la BD.
/// </summary>
public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) GenerateAccessToken(User user);

    /// <summary>
    /// Devuelve una tupla (tokenPlano, hash, fechaExpiracion). El hash es el
    /// que se guarda en BD; el token plano solo se devuelve al cliente una vez.
    /// </summary>
    (string Token, string TokenHash, DateTime ExpiresAt) GenerateRefreshToken();

    /// <summary>
    /// Hashea un refresh token recibido del cliente para buscarlo en BD.
    /// </summary>
    string HashRefreshToken(string token);
}
