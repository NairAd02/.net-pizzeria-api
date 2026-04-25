using System.ComponentModel.DataAnnotations;

namespace Pizzeria.API.Modules.Auth.Dtos;

public record SignUpDto(
    [property: Required, EmailAddress] string Email,
    [property: Required, MinLength(8)] string Password);
