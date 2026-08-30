using GigHub.Api.Dtos;

namespace GigHub.Api.Services;

/// <summary>Registrering og login (gang 06). Udsteder JWT'er - se AuthService for detaljer.</summary>
public interface IAuthService
{
    /// <exception cref="GigHub.Api.Common.Exceptions.ConflictException">Email er allerede i brug.</exception>
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default);

    /// <exception cref="UnauthorizedAccessException">Forkert email eller adgangskode.</exception>
    Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default);
}
