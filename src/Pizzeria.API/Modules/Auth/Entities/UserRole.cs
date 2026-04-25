namespace Pizzeria.API.Modules.Auth.Entities;

/// <summary>
/// Roles de la aplicación. Se persiste como string (igual que <c>OrderStatus</c>)
/// y se emite como claim <c>role</c> dentro del JWT, para que
/// <c>[Authorize(Roles = "Admin")]</c> funcione sin configuración extra.
/// </summary>
public enum UserRole
{
    Admin,
    Client,
}
