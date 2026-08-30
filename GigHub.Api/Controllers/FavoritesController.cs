using GigHub.Api.Common;
using GigHub.Api.Common.Exceptions;
using GigHub.Api.Data;
using GigHub.Api.Dtos;
using GigHub.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GigHub.Api.Controllers;

/// <summary>
/// Favoritmarkering (mange-til-mange User &lt;-&gt; Event) - gang 08 i design-brief.md.
/// Logikken er simpel nok til at ligge direkte i controlleren frem for i et separat
/// service-lag (i modsætning til Booking/Review, der har rigtig forretningslogik at teste).
/// </summary>
[ApiController]
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly GighubDbContext _context;

    public FavoritesController(GighubDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// GET /api/favorites - understøttende læse-endpoint (ikke eksplicit i design-briefens
    /// endpoint-tabel), som favorit-hjerte-knappen i Angular bruger til at vise sin initiale
    /// "udfyldt/tom"-state, uden at skulle tjekke hvert enkelt event for sig.
    /// </summary>
    [HttpGet("/api/favorites")]
    public async Task<ActionResult<IReadOnlyList<FavoriteDto>>> GetMyFavorites(CancellationToken ct)
    {
        var userId = User.GetUserId();

        // Ingen eksplicit .Include(f => f.Event) - EF Core oversætter adgangen til f.Event.Title
        // i Select-projektionen til en JOIN, se samme bemærkning i ReviewService.GetReviewsForEventAsync.
        var favorites = await _context.Favorites
            .Where(f => f.UserId == userId)
            .Select(f => new FavoriteDto(f.UserId, f.EventId, f.Event!.Title, f.CreatedAt))
            .ToListAsync(ct);

        return Ok(favorites);
    }

    [HttpPost("/api/events/{eventId:int}/favorites")]
    public async Task<IActionResult> AddFavorite(int eventId, CancellationToken ct)
    {
        var userId = User.GetUserId();

        var eventExists = await _context.Events.AnyAsync(e => e.Id == eventId, ct);
        if (!eventExists)
        {
            throw new NotFoundException($"Event med id {eventId} findes ikke.");
        }

        var alreadyFavorite = await _context.Favorites
            .AnyAsync(f => f.UserId == userId && f.EventId == eventId, ct);

        if (alreadyFavorite)
        {
            // Idempotent: at favoritmarkere noget, der allerede er favoritmarkeret, er ikke en
            // fejl - det er den tilstand, klienten bad om. Undgår en unødvendig 409 for noget,
            // der reelt ikke er en konflikt for brugeren (i modsætning til dobbelt-booking).
            return NoContent();
        }

        _context.Favorites.Add(new Favorite { UserId = userId, EventId = eventId, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpDelete("/api/events/{eventId:int}/favorites")]
    public async Task<IActionResult> RemoveFavorite(int eventId, CancellationToken ct)
    {
        var userId = User.GetUserId();

        var favorite = await _context.Favorites.FindAsync(new object[] { userId, eventId }, ct);
        if (favorite is null)
        {
            return NoContent(); // allerede fjernet - idempotent, samme begrundelse som ovenfor
        }

        _context.Favorites.Remove(favorite);
        await _context.SaveChangesAsync(ct);

        return NoContent();
    }
}
