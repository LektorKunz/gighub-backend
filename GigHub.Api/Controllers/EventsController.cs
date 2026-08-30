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
/// CRUD + søgning/filtrering/paginering for events. Startede i gang 01 som en hardcodet
/// List&lt;Event&gt; direkte i controlleren (ingen DB, ingen DTO'er) og er gradvist bygget ud til
/// denne version - se design-brief.md afsnit 4, "Endpoints pr. gang", for den fulde rejse.
/// </summary>
[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly GighubDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public EventsController(GighubDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    /// <summary>
    /// GET /api/events?genre=&amp;search=&amp;page=&amp;pageSize= - offentligt tilgængeligt, ingen [Authorize].
    /// Filtrering og paginering blev tilføjet i gang 05 (design-brief.md).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<EventDto>>> GetEvents(
        [FromQuery] EventGenre? genre,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        // Forsvar mod useligt/ondsindet input - fx page=0 eller pageSize=100000.
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        // Ingen eksplicit .Include(e => e.Arrangoer) - EF Core oversætter adgangen til
        // e.Arrangoer.Name i Select-projektionen nedenfor til en JOIN, se samme bemærkning
        // i ReviewService.GetReviewsForEventAsync.
        var query = _context.Events.AsNoTracking().AsQueryable();

        if (genre.HasValue)
        {
            query = query.Where(e => e.Genre == genre.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            // EF.Functions.Like oversættes til SQL LIKE - se bro-tabellen "Rå SQL → EF Core"
            // i design-brief.md, punktet om at LINQ oversættes til SQL bag kulisserne.
            query = query.Where(e =>
                EF.Functions.Like(e.Title, $"%{term}%") ||
                EF.Functions.Like(e.VenueName, $"%{term}%"));
        }

        var totalCount = await query.CountAsync(ct);

        var events = await query
            .OrderBy(e => e.DateTimeUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EventDto(
                e.Id, e.Title, e.Description, e.Genre, e.VenueName, e.Address,
                e.DateTimeUtc, e.Capacity, e.ImageUrl, e.ArrangoerId,
                e.Arrangoer!.Name, e.CreatedAt))
            .ToListAsync(ct);

        return Ok(new PagedResult<EventDto>(events, page, pageSize, totalCount));
    }

    /// <summary>
    /// GET /api/events/{id} - offentligt tilgængeligt (ingen [Authorize]), men beriget med
    /// MyBookingStatus og IsFavorite, hvis requesten alligevel har et gyldigt JWT med (Angular's
    /// interceptor sender token på alle kald fra gang 06 og frem - se ClaimsPrincipalExtensions).
    /// Inkluderer BookedCount, AverageRating og Reviews siden gang 07, IsFavorite siden gang 08.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<EventDetailDto>> GetEvent(int id, CancellationToken ct)
    {
        var gigEvent = await _context.Events
            .AsNoTracking()
            .Include(e => e.Arrangoer)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (gigEvent is null)
        {
            throw new NotFoundException($"Event med id {id} findes ikke.");
        }

        var bookedCount = await _context.Bookings
            .CountAsync(b => b.EventId == id && b.Status == BookingStatus.Booket, ct);

        var reviews = await _context.Reviews
            .AsNoTracking()
            .Where(r => r.EventId == id)
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewDto(r.Id, r.EventId, r.UserId, r.User!.Name, r.Rating, r.Comment, r.CreatedAt))
            .ToListAsync(ct);

        double? averageRating = reviews.Count > 0 ? reviews.Average(r => r.Rating) : null;

        BookingStatus? myBookingStatus = null;
        var isFavorite = false;

        if (User.TryGetUserId(out var currentUserId))
        {
            myBookingStatus = await _context.Bookings
                .Where(b => b.EventId == id && b.UserId == currentUserId)
                .Select(b => (BookingStatus?)b.Status)
                .FirstOrDefaultAsync(ct);

            isFavorite = await _context.Favorites
                .AnyAsync(f => f.EventId == id && f.UserId == currentUserId, ct);
        }

        return Ok(new EventDetailDto(
            gigEvent.Id, gigEvent.Title, gigEvent.Description, gigEvent.Genre, gigEvent.VenueName,
            gigEvent.Address, gigEvent.DateTimeUtc, gigEvent.Capacity, gigEvent.ImageUrl,
            gigEvent.ArrangoerId, gigEvent.Arrangoer!.Name, gigEvent.CreatedAt,
            bookedCount, averageRating, reviews.Count, reviews, myBookingStatus, isFavorite));
    }

    /// <summary>POST /api/events - kun Arrangør/Admin. ArrangoerId sættes fra JWT'et, aldrig fra request-body.</summary>
    [HttpPost]
    [Authorize(Roles = "Arrangoer,Admin")]
    public async Task<ActionResult<EventDto>> CreateEvent(EventCreateDto dto, CancellationToken ct)
    {
        var userId = User.GetUserId();

        var gigEvent = new Event
        {
            Title = dto.Title,
            Description = dto.Description,
            Genre = dto.Genre,
            VenueName = dto.VenueName,
            Address = dto.Address,
            DateTimeUtc = dto.DateTimeUtc,
            Capacity = dto.Capacity,
            ImageUrl = dto.ImageUrl,
            ArrangoerId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Events.Add(gigEvent);
        await _context.SaveChangesAsync(ct);

        // ClaimTypes.Name-claim'et (sat i AuthService.GenerateAuthResponse) læses automatisk
        // ind i User.Identity.Name af JWT-middlewaren.
        var arrangoerName = User.Identity?.Name ?? string.Empty;

        var result = new EventDto(gigEvent.Id, gigEvent.Title, gigEvent.Description, gigEvent.Genre,
            gigEvent.VenueName, gigEvent.Address, gigEvent.DateTimeUtc, gigEvent.Capacity,
            gigEvent.ImageUrl, gigEvent.ArrangoerId, arrangoerName, gigEvent.CreatedAt);

        return CreatedAtAction(nameof(GetEvent), new { id = gigEvent.Id }, result);
    }

    /// <summary>PUT /api/events/{id} - kun eventets egen arrangør eller en Admin.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Arrangoer,Admin")]
    public async Task<IActionResult> UpdateEvent(int id, EventUpdateDto dto, CancellationToken ct)
    {
        var gigEvent = await _context.Events.FindAsync(new object[] { id }, ct)
            ?? throw new NotFoundException($"Event med id {id} findes ikke.");

        EnsureOwnerOrAdmin(gigEvent);

        gigEvent.Title = dto.Title;
        gigEvent.Description = dto.Description;
        gigEvent.Genre = dto.Genre;
        gigEvent.VenueName = dto.VenueName;
        gigEvent.Address = dto.Address;
        gigEvent.DateTimeUtc = dto.DateTimeUtc;
        gigEvent.Capacity = dto.Capacity;
        gigEvent.ImageUrl = dto.ImageUrl;

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>DELETE /api/events/{id} - kun eventets egen arrangør eller en Admin.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Arrangoer,Admin")]
    public async Task<IActionResult> DeleteEvent(int id, CancellationToken ct)
    {
        var gigEvent = await _context.Events.FindAsync(new object[] { id }, ct)
            ?? throw new NotFoundException($"Event med id {id} findes ikke.");

        EnsureOwnerOrAdmin(gigEvent);

        _context.Events.Remove(gigEvent);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>
    /// POST /api/events/{id}/image - filupload (gang 08). Gemmer filen i wwwroot/uploads/events/
    /// og sætter Event.ImageUrl til den offentlige sti, som app.UseStaticFiles() servererer fra.
    /// </summary>
    [HttpPost("{id:int}/image")]
    [Authorize(Roles = "Arrangoer,Admin")]
    [RequestSizeLimit(5 * 1024 * 1024)] // 5 MB
    public async Task<ActionResult<EventDto>> UploadImage(int id, IFormFile file, CancellationToken ct)
    {
        var gigEvent = await _context.Events
            .Include(e => e.Arrangoer)
            .FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new NotFoundException($"Event med id {id} findes ikke.");

        EnsureOwnerOrAdmin(gigEvent);

        if (file.Length == 0)
        {
            throw new ConflictException("Filen er tom.");
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            throw new ConflictException("Kun billedfiler (jpg, jpeg, png, webp) er tilladt.");
        }

        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var uploadsFolder = Path.Combine(webRoot, "uploads", "events");
        Directory.CreateDirectory(uploadsFolder);

        // Guid i filnavnet undgår navnekollisioner og forhindrer, at et uploadet filnavn kan
        // bruges til at overskrive en anden fil på serveren (path traversal-lignende angreb).
        var fileName = $"{gigEvent.Id}-{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, ct);
        }

        gigEvent.ImageUrl = $"/uploads/events/{fileName}";
        await _context.SaveChangesAsync(ct);

        var result = new EventDto(gigEvent.Id, gigEvent.Title, gigEvent.Description, gigEvent.Genre,
            gigEvent.VenueName, gigEvent.Address, gigEvent.DateTimeUtc, gigEvent.Capacity,
            gigEvent.ImageUrl, gigEvent.ArrangoerId, gigEvent.Arrangoer?.Name ?? string.Empty, gigEvent.CreatedAt);

        return Ok(result);
    }

    private void EnsureOwnerOrAdmin(Event gigEvent)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsInRole(nameof(UserRole.Admin));

        if (!isAdmin && gigEvent.ArrangoerId != userId)
        {
            throw new ForbiddenException("Du kan kun redigere eller slette dine egne events.");
        }
    }
}
