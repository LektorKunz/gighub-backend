namespace GigHub.Api.Common.Exceptions;

// Domæne-exceptions, som services kaster, og som GigHub.Api.Middleware.ExceptionHandlingMiddleware
// oversætter til de rigtige HTTP-statuskoder + ProblemDetails. Formålet er at holde controllere
// og services fri for manuelt "if (...) return StatusCode(...)"-kode overalt - man kaster i
// stedet en meningsfuld exception, og middlewaren tager sig af HTTP-oversættelsen ét sted.

/// <summary>Den efterspurgte ressource (event, booking, ...) findes ikke. Oversættes til 404.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}

/// <summary>
/// Operationen er i konflikt med den nuværende tilstand (dobbelt-booking, email allerede i
/// brug, dobbelt anmeldelse, ...). Oversættes til 409 - se forretningsregel 3 i design-brief.md
/// for hvorfor dette specifikt er 409 og ikke det rå 500, man ellers ville få fra en
/// UNIQUE constraint-fejl direkte fra SQLite.
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}

/// <summary>
/// Brugeren er logget ind (autentificeret), men har ikke ret til den konkrete handling
/// (fx redigere andres event, anmelde et event man ikke har været booket til). Oversættes til 403.
/// Adskilt fra 401 Unauthorized, som betyder "vi ved slet ikke, hvem du er".
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message)
    {
    }
}
