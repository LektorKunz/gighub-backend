using System.ComponentModel.DataAnnotations;
using GigHub.Api.Models;

namespace GigHub.Api.Dtos;

/// <summary>
/// Selv-registrering opretter altid en <see cref="UserRole.Deltager"/> - Arrangoer/Admin-roller
/// tildeles ikke via det offentlige registrerings-endpoint (ville ellers lade enhver gøre sig selv
/// til admin). I dette forløb seedes Arrangoer/Admin-brugere i stedet via DbSeeder.
/// </summary>
public record RegisterDto(
    [property: Required, MaxLength(100)] string Name,
    [property: Required, EmailAddress, MaxLength(200)] string Email,
    [property: Required, MinLength(8), MaxLength(100)] string Password);

public record LoginDto(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password);

/// <summary>Svar fra både register og login - Angular's AuthService gemmer Token og bruger resten til UI-state.</summary>
public record AuthResponseDto(
    string Token,
    DateTime ExpiresAtUtc,
    int UserId,
    string Name,
    string Email,
    UserRole Role);
