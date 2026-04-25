namespace Pizzeria.API.Modules.Auth.Entities;

/// <summary>
/// Usuario de la aplicación. Mantenemos una tabla propia (en vez de usar
/// ASP.NET Core Identity completo) para encajar con el estilo modular del repo.
/// El hash de la contraseña se genera con <c>PasswordHasher&lt;User&gt;</c>
/// (PBKDF2 + salt por usuario), que es el mismo algoritmo que usa Identity.
/// </summary>
public class User
{
    public required Guid Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public UserRole Role { get; set; } = UserRole.Client;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
