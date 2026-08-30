using GigHub.Api.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GigHub.Api.Middleware;

/// <summary>
/// Global fejlhåndtering (gang 07 i design-brief.md). Fanger ALLE ubehandlede exceptions,
/// der bobler op igennem controllere/services, og oversætter dem til et ensartet
/// <c>ProblemDetails</c>-svar (RFC 7807) i stedet for en rå 500 med en stacktrace.
///
/// Ligger som det allerførste led i pipelinen i Program.cs, så den kan indfange fejl fra
/// alt, hvad der kører efter den.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Ressourcen blev ikke fundet"),
            ConflictException => (StatusCodes.Status409Conflict, "Konflikt med den nuværende tilstand"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Ikke tilladt"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Ikke godkendt"),

            // Sikkerhedsnet: hvis en UNIQUE constraint-fejl (fx dobbelt-booking, forretningsregel 3)
            // af en eller anden grund IKKE er blevet fanget og oversat inde i et service-lag,
            // ender den her som en pæn 409 i stedet for en rå 500 - det er selve pointen i
            // "produktiv fejl"-øjeblikket i gang 07: sammenlign med hvordan den så ud i gang 04.
            DbUpdateException => (StatusCodes.Status409Conflict, "Databasekonflikt - ressourcen findes muligvis allerede"),

            _ => (StatusCodes.Status500InternalServerError, "Der opstod en uventet serverfejl")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Ubehandlet exception under behandling af {Method} {Path}",
                context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogWarning(exception, "Håndteret fejl ({StatusCode}) under {Method} {Path}",
                statusCode, context.Request.Method, context.Request.Path);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            // I en rigtig produktions-app ville man overveje at IKKE eksponere exception.Message
            // for 500-fejl (kan lække interne detaljer) - her er det bevidst holdt simpelt til
            // undervisningsbrug, da det gør fejlsøgning under øvelser langt hurtigere.
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
