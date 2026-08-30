using System.ComponentModel.DataAnnotations;
using GigHub.Api.Models;

namespace GigHub.Api.Dtos;

/// <summary>
/// DTO'er for Event. EF-entiteten <see cref="Event"/> eksponeres ALDRIG direkte i API'et -
/// dels for ikke at lække interne felter (fx en fremtidig soft-delete-flag), dels fordi
/// navigation properties (Arrangoer, Bookings, ...) ville give uendelig/uønsket JSON-serialisering,
/// hvis man ikke er varsom med Include().
/// </summary>

/// <summary>Bruges i event-listen (GET /api/events) - uden anmeldelses-gennemsnit af hensyn til performance.</summary>
public record EventDto(
    int Id,
    string Title,
    string Description,
    EventGenre Genre,
    string VenueName,
    string Address,
    DateTime DateTimeUtc,
    int Capacity,
    string? ImageUrl,
    int ArrangoerId,
    string ArrangoerName,
    DateTime CreatedAt);

/// <summary>
/// Bruges på detalje-endpointet (GET /api/events/{id}) - udvider EventDto med antal booket-pladser
/// og gennemsnitsrating, jf. gang 07 i design-brief.md ("GET /api/events/{id} inkl. gennemsnitsrating").
/// </summary>
public record EventDetailDto(
    int Id,
    string Title,
    string Description,
    EventGenre Genre,
    string VenueName,
    string Address,
    DateTime DateTimeUtc,
    int Capacity,
    string? ImageUrl,
    int ArrangoerId,
    string ArrangoerName,
    DateTime CreatedAt,
    int BookedCount,
    /// <summary>Null, hvis eventet endnu ikke har nogen anmeldelser.</summary>
    double? AverageRating,
    int ReviewCount,
    /// <summary>Alle anmeldelser af eventet, jf. gang 07 ("en liste af Reviews").</summary>
    IReadOnlyList<ReviewDto> Reviews,
    /// <summary>
    /// Null hvis besøgende ikke er logget ind, eller ikke har en booking på eventet. Bruges i
    /// Angular til at afgøre, om "skriv anmeldelse"-knappen skal vises (kun hvis Booket og
    /// eventet er overstået - selve forretningsreglen håndhæves stadig server-side i
    /// ReviewsController, se gang 07 "Bemærk til underviseren": UI-kontrol er ikke sikkerhed).
    /// </summary>
    BookingStatus? MyBookingStatus,
    /// <summary>False hvis besøgende ikke er logget ind. Jf. gang 08.</summary>
    bool IsFavorite);

public record EventCreateDto(
    [property: Required, MaxLength(200)] string Title,
    [property: Required, MaxLength(2000)] string Description,
    EventGenre Genre,
    [property: Required, MaxLength(200)] string VenueName,
    [property: Required, MaxLength(300)] string Address,
    DateTime DateTimeUtc,
    [property: Range(1, 100_000)] int Capacity,
    string? ImageUrl);

public record EventUpdateDto(
    [property: Required, MaxLength(200)] string Title,
    [property: Required, MaxLength(2000)] string Description,
    EventGenre Genre,
    [property: Required, MaxLength(200)] string VenueName,
    [property: Required, MaxLength(300)] string Address,
    DateTime DateTimeUtc,
    [property: Range(1, 100_000)] int Capacity,
    string? ImageUrl);
