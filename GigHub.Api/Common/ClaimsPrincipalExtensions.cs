using System.Security.Claims;

namespace GigHub.Api.Common;

/// <summary>
/// Hjælpemetode til at læse det indloggede bruger-id ud af JWT-claims (<c>ClaimTypes.NameIdentifier</c>).
/// Fra gang 06 er dette den eneste kilde til "hvem er brugeren", i stedet for et UserId sendt i
/// request-body (den midlertidige fake-bruger-løsning fra gang 04) - se design-brief.md afsnit 4,
/// "Bevidst pædagogisk stilladsering".
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (value is null || !int.TryParse(value, out var userId))
        {
            // Sker kun, hvis nogen har konstrueret et gyldigt signeret token uden det
            // forventede claim - bør praktisk talt aldrig ramme uden for udvikling/test.
            throw new UnauthorizedAccessException("Token indeholder ikke et gyldigt bruger-id.");
        }

        return userId;
    }

    /// <summary>
    /// Sikker variant til endpoints uden <c>[Authorize]</c> (fx GET /api/events/{id}, som skal
    /// virke for ikke-loggede-ind besøgende, jf. gang 07/08): returnerer <c>false</c> i stedet for
    /// at kaste, hvis der ikke er noget (gyldigt) bruger-claim - fx fordi requesten slet ikke har
    /// et Authorization-header med.
    /// </summary>
    public static bool TryGetUserId(this ClaimsPrincipal principal, out int userId)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (value is null || !int.TryParse(value, out userId))
        {
            userId = 0;
            return false;
        }

        return true;
    }
}
