using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GigHub.Api.Common.Exceptions;
using GigHub.Api.Data;
using GigHub.Api.Dtos;
using GigHub.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace GigHub.Api.Services;

/// <summary>
/// Registrering/login og JWT-udstedelse (gang 06 i design-brief.md). Bruger den indbyggede
/// <see cref="PasswordHasher{TUser}"/> (kommer med ASP.NET Core's delte framework, kræver ingen
/// ekstra NuGet-pakke) i stedet for fuld ASP.NET Core Identity - se "Ikke fuld ASP.NET Core
/// Identity"-bemærkningen i 00-oversigt-og-underviserguide.md for begrundelsen.
/// </summary>
public class AuthService : IAuthService
{
    private readonly GighubDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthService(GighubDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
    {
        var emailInUse = await _context.Users.AnyAsync(u => u.Email == dto.Email, ct);
        if (emailInUse)
        {
            throw new ConflictException($"Email '{dto.Email}' er allerede i brug.");
        }

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = string.Empty, // sættes lige nedenfor - HashPassword skal bruge et User-objekt
            Role = UserRole.Deltager
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);

        return GenerateAuthResponse(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == dto.Email, ct);

        // Bevidst samme fejlbesked, uanset om det er emailen eller adgangskoden, der er forkert -
        // ellers kan man bruge login-endpointet til at afsløre, hvilke emails der er registreret.
        if (user is null)
        {
            throw new UnauthorizedAccessException("Forkert email eller adgangskode.");
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException("Forkert email eller adgangskode.");
        }

        return GenerateAuthResponse(user);
    }

    private AuthResponseDto GenerateAuthResponse(User user)
    {
        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key mangler i konfigurationen (appsettings.json).");
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        var expiryMinutes = _configuration.GetValue("Jwt:ExpiryMinutes", 120);

        var claims = new List<Claim>
        {
            // ClaimTypes.NameIdentifier er den claim, ClaimsPrincipalExtensions.GetUserId()
            // og [Authorize(Roles = ...)] læser bruger-id og rolle fra på alle beskyttede endpoints.
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new AuthResponseDto(tokenString, expiresAtUtc, user.Id, user.Name, user.Email, user.Role);
    }
}
