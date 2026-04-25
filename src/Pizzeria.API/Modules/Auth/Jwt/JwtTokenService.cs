using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Pizzeria.API.Modules.Auth.Entities;

namespace Pizzeria.API.Modules.Auth.Jwt;

public class JwtTokenService(JwtOptions options) : IJwtTokenService
{
    private readonly SigningCredentials _signingCredentials = new(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Secret)),
        SecurityAlgorithms.HmacSha256);

    public (string Token, DateTime ExpiresAt) GenerateAccessToken(User user)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(options.AccessTokenMinutes);

        // Claims estándar. ClaimTypes.Role se traduce al claim "role" y lo
        // lee [Authorize(Roles = "...")] sin configuración extra.
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: _signingCredentials);

        var serialized = new JwtSecurityTokenHandler().WriteToken(token);
        return (serialized, expiresAt);
    }

    public (string Token, string TokenHash, DateTime ExpiresAt) GenerateRefreshToken()
    {
        // 64 bytes de entropía ⇒ 88 chars en Base64Url. Más que suficiente.
        var bytes = RandomNumberGenerator.GetBytes(64);
        var token = Base64UrlEncoder.Encode(bytes);
        var hash = HashRefreshToken(token);
        var expiresAt = DateTime.UtcNow.AddDays(options.RefreshTokenDays);
        return (token, hash, expiresAt);
    }

    public string HashRefreshToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
