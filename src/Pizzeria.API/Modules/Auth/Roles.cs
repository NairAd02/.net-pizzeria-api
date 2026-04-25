using Pizzeria.API.Modules.Auth.Entities;

namespace Pizzeria.API.Modules.Auth;

/// <summary>
/// Constantes de rol usables dentro de <c>[Authorize(Roles = ...)]</c>.
/// Están atadas al enum <see cref="UserRole"/> vía <c>nameof</c>, así que si
/// renombras un valor del enum, el compilador obliga a actualizar estas constantes.
/// </summary>
public static class Roles
{
    public const string Admin = nameof(UserRole.Admin);
    public const string Client = nameof(UserRole.Client);
}
