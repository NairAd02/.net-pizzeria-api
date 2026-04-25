using System.ComponentModel.DataAnnotations;

namespace Pizzeria.API.Modules.Auth.Dtos;

public record RefreshDto([property: Required] string RefreshToken);
