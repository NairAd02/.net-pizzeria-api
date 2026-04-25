using Pizzeria.API.Modules.Auth.Dtos;

namespace Pizzeria.API.Modules.Auth;

public interface IAuthService
{
    Task<AuthResponseDto> SignUpAsync(SignUpDto dto, CancellationToken ct = default);
    Task<AuthResponseDto> SignInAsync(SignInDto dto, CancellationToken ct = default);
    Task<AuthResponseDto> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task SignOutAsync(string refreshToken, CancellationToken ct = default);
    Task<MeDto?> GetMeAsync(Guid userId, CancellationToken ct = default);
}
