using System.Data;
using GigHub.Api.Common.Exceptions;
using GigHub.Api.Data;
using GigHub.Api.Dtos;
using GigHub.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GigHub.Api.Services;

/// <summary>
/// FACIT-implementation af kapacitets-/venteliste-logikken. Dette er bevidst IKKE den naive
/// version, de studerende bygger først i gang 04 ("tæl bookinger, sammenlign med kapacitet,
/// indsæt") - den version har en race condition, hvis to requests rammer serveren i samme
/// øjeblik: begge kan nå at tælle det samme antal ledige pladser, før nogen af dem har gemt
/// deres egen booking, og begge ender med Status = Booket, selvom kun én plads var tilbage.
///
/// Løsningen her (gang 05, forretningsregel 1 i design-brief.md) gør "tæl og indsæt" atomisk
/// ved at pakke det ind i én databasetransaktion.
/// </summary>
public class BookingService : IBookingService
{
    private readonly GighubDbContext _context;

    public BookingService(GighubDbContext context)
    {
        _context = context;
    }

    public async Task<BookingDto> CreateBookingAsync(int eventId, int userId, CancellationToken ct = default)
    {
        var gigEvent = await _context.Events.FindAsync(new object[] { eventId }, ct)
            ?? throw new NotFoundException($"Event med id {eventId} findes ikke.");

        // EF Core's InMemory-provider (bruges kun i GigHub.Api.Tests) understøtter ikke
        // eksplicitte transaktioner og kaster en InvalidOperationException, hvis man forsøger.
        // I produktion (SQLite, se appsettings.json) er IsRelational() altid true, så
        // transaktionen bruges reelt hver gang applikationen kører.
        var useTransaction = _context.Database.IsRelational();
        IDbContextTransaction? transaction = null;

        if (useTransaction)
        {
            // Microsoft.Data.Sqlite understøtter kun IsolationLevel.Serializable og
            // ReadUncommitted - Serializable er også dens standard. Vi sætter den eksplicit
            // for at gøre hensigten tydelig i koden: "tæl eksisterende bookinger" og "indsæt
            // den nye booking" skal opføre sig som ÉN atomisk operation.
            transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        }

        try
        {
            var alreadyBooked = await _context.Bookings
                .AnyAsync(b => b.EventId == eventId && b.UserId == userId, ct);

            if (alreadyBooked)
            {
                throw new ConflictException(
                    $"Bruger {userId} har allerede en booking på event {eventId}.");
            }

            var bookedCount = await _context.Bookings
                .CountAsync(b => b.EventId == eventId && b.Status == BookingStatus.Booket, ct);

            var status = bookedCount < gigEvent.Capacity
                ? BookingStatus.Booket
                : BookingStatus.Venteliste;

            var booking = new Booking
            {
                EventId = eventId,
                UserId = userId,
                Status = status,
                CreatedAt = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);

            try
            {
                await _context.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                // Sikkerhedsnet: rammer kun i det meget smalle vindue, hvor to samtidige
                // transaktioner begge er nået forbi AnyAsync-tjekket ovenfor, før nogen af dem
                // har committet. Den unikke (EventId, UserId)-constraint i GighubDbContext
                // fanger det stadig på databaseniveau, og vi oversætter det til en pæn 409
                // i stedet for at lade den rå exception boble op.
                throw new ConflictException(
                    $"Bruger {userId} har allerede en booking på event {eventId}.");
            }

            if (transaction is not null)
            {
                await transaction.CommitAsync(ct);
            }

            return new BookingDto(booking.Id, booking.EventId, booking.UserId, booking.Status, booking.CreatedAt);
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(ct);
            }

            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    public async Task<IReadOnlyList<BookingDto>> GetBookingsForUserAsync(int userId, CancellationToken ct = default)
    {
        return await _context.Bookings
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BookingDto(b.Id, b.EventId, b.UserId, b.Status, b.CreatedAt))
            .ToListAsync(ct);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        // OBS: denne tekst-baserede detektion er specifik for SQLite's fejlbesked
        // ("UNIQUE constraint failed: ..."). Skifter man til en anden provider (fx SQL Server,
        // se bemærkningen i design-brief.md afsnit 3), skal den erstattes med et opslag på
        // SqlException.Number (2601 eller 2627) i stedet.
        return ex.InnerException?.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) == true;
    }
}
