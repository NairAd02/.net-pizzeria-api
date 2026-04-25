using System.ComponentModel.DataAnnotations;

namespace Pizzeria.API.Modules.Auth.Dtos;

public record SignInDto(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password);
