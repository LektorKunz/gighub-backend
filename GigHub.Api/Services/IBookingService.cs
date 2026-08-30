using GigHub.Api.Dtos;

namespace GigHub.Api.Services;

/// <summary>
/// Kapacitets-/venteliste-logikken for bookinger (forretningsregel 1 i design-brief.md).
/// Ligger i et service-lag - ikke direkte i BookingsController - netop så den kan
/// unit-testes uafhængigt af HTTP-pipelinen, se GigHub.Api.Tests/BookingServiceTests.cs.
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Opretter en booking for <paramref name="userId"/> på eventet <paramref name="eventId"/>.
    /// Status bliver Booket, hvis der er ledig plads, ellers Venteliste.
    /// </summary>
    /// <exception cref="GigHub.Api.Common.Exceptions.NotFoundException">Eventet findes ikke.</exception>
    /// <exception cref="GigHub.Api.Common.Exceptions.ConflictException">Brugeren har allerede en booking på eventet.</exception>
    Task<BookingDto> CreateBookingAsync(int eventId, int userId, CancellationToken ct = default);

    /// <summary>Alle bookinger for den givne bruger, nyeste først.</summary>
    Task<IReadOnlyList<BookingDto>> GetBookingsForUserAsync(int userId, CancellationToken ct = default);
}
