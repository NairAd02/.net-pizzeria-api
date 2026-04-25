using Pizzeria.API.Modules.Auth.Entities;

namespace Pizzeria.API.Modules.Auth.Dtos;

public record MeDto(Guid Id, string Email, UserRole Role, DateTime CreatedAt);
