using GigHub.Api.Common;
using GigHub.Api.Dtos;
using GigHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GigHub.Api.Controllers;

/// <summary>
/// Booking-endpoints, nested under events (POST /api/events/{eventId}/bookings).
/// Introduceret i gang 04 med en midlertidig fake-bruger (UserId i request-body) og
/// refaktoreret i gang 06 til at bruge det ægte, indloggede bruger-id fra JWT'et -
/// se design-brief.md afsnit 4, "Bevidst pædagogisk stilladsering". Denne facit-version
/// viser slutresultatet: [Authorize] + User.GetUserId(), intet UserId i request-bodyen.
/// </summary>
[ApiController]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpPost("/api/events/{eventId:int}/bookings")]
    public async Task<ActionResult<BookingDto>> CreateBooking(int eventId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var booking = await _bookingService.CreateBookingAsync(eventId, userId, ct);

        // Der findes bevidst intet GET-endpoint for en enkelt booking (kun listen "mine"
        // bookinger nedenfor), så vi returnerer 201 uden et Location-peg på en bestemt URI.
        return StatusCode(StatusCodes.Status201Created, booking);
    }

    /// <summary>
    /// GET /api/bookings/mine - understøttende læse-endpoint (ikke eksplicit nævnt i
    /// design-briefens endpoint-tabel), som Angular's BookingButtonComponent har brug for
    /// for at kunne vise "du er allerede booket/på venteliste" for et event.
    /// </summary>
    [HttpGet("/api/bookings/mine")]
    public async Task<ActionResult<IReadOnlyList<BookingDto>>> GetMyBookings(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var bookings = await _bookingService.GetBookingsForUserAsync(userId, ct);
        return Ok(bookings);
    }
}
