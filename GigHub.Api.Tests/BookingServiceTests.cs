using GigHub.Api.Common.Exceptions;
using GigHub.Api.Data;
using GigHub.Api.Models;
using GigHub.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GigHub.Api.Tests;

/// <summary>
/// Tester BookingService's kapacitets-/venteliste-logik (forretningsregel 1 i design-brief.md)
/// med EF Core's InMemory-provider - matcher gang 09's emne ("test med xUnit").
///
/// OBS, vigtigt at sige højt for holdet: InMemory-provideren understøtter ikke transaktioner,
/// så disse tests dækker IKKE selve race condition-scenariet (to samtidige requests). De
/// dækker forretningsreglen "korrekt status ud fra kapacitet og eksisterende bookinger",
/// som er det, der faktisk kan enhedstestes uden en rigtig, samtidig databaseforbindelse.
/// BookingService.CreateBookingAsync håndterer selv dette (se dens IsRelational()-tjek) og
/// springer transaktionslogikken over, når den kører mod InMemory.
/// </summary>
public class BookingServiceTests
{
    private static GighubDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<GighubDbContext>()
            // Unikt databasenavn pr. test sikrer isolation mellem tests, selv hvis xUnit
            // kører dem parallelt.
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new GighubDbContext(options);
    }

    private static async Task<int> SeedEventAsync(GighubDbContext context, int capacity)
    {
        var arrangoer = new User
        {
            Name = "Arne Arrangør",
            Email = $"arne-{Guid.NewGuid():N}@example.com",
            PasswordHash = "irrelevant-for-denne-test"
        };
        context.Users.Add(arrangoer);
        await context.SaveChangesAsync();

        var gigEvent = new Event
        {
            Title = "Testkoncert",
            Description = "En koncert oprettet til test",
            Genre = EventGenre.Koncert,
            VenueName = "Testhallen",
            Address = "Testvej 1, 8000 Aarhus C",
            DateTimeUtc = DateTime.UtcNow.AddDays(7),
            Capacity = capacity,
            ArrangoerId = arrangoer.Id
        };
        context.Events.Add(gigEvent);
        await context.SaveChangesAsync();

        return gigEvent.Id;
    }

    private static async Task<int> SeedUserAsync(GighubDbContext context, string name = "Deltager")
    {
        var user = new User
        {
            Name = name,
            Email = $"{name.ToLowerInvariant()}-{Guid.NewGuid():N}@example.com",
            PasswordHash = "irrelevant-for-denne-test"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user.Id;
    }

    [Fact]
    public async Task CreateBookingAsync_NaarDerErLedigPlads_SaetterStatusBooket()
    {
        await using var context = CreateContext();
        var eventId = await SeedEventAsync(context, capacity: 2);
        var userId = await SeedUserAsync(context);
        var sut = new BookingService(context);

        var booking = await sut.CreateBookingAsync(eventId, userId);

        Assert.Equal(BookingStatus.Booket, booking.Status);
    }

    [Fact]
    public async Task CreateBookingAsync_NaarKapacitetErFyldt_SaetterStatusVenteliste()
    {
        await using var context = CreateContext();
        var eventId = await SeedEventAsync(context, capacity: 1);
        var sut = new BookingService(context);

        var foersteBrugerId = await SeedUserAsync(context, "Foerste");
        var andenBrugerId = await SeedUserAsync(context, "Anden");

        var foerste = await sut.CreateBookingAsync(eventId, foersteBrugerId);
        var anden = await sut.CreateBookingAsync(eventId, andenBrugerId);

        Assert.Equal(BookingStatus.Booket, foerste.Status);
        Assert.Equal(BookingStatus.Venteliste, anden.Status);
    }

    [Fact]
    public async Task CreateBookingAsync_TredjeBookingNaarKapacitetErToOgToErBooket_GaarPaaVenteliste()
    {
        await using var context = CreateContext();
        var eventId = await SeedEventAsync(context, capacity: 2);
        var sut = new BookingService(context);

        await sut.CreateBookingAsync(eventId, await SeedUserAsync(context, "Foerste"));
        await sut.CreateBookingAsync(eventId, await SeedUserAsync(context, "Anden"));
        var tredje = await sut.CreateBookingAsync(eventId, await SeedUserAsync(context, "Tredje"));

        Assert.Equal(BookingStatus.Venteliste, tredje.Status);
    }

    [Fact]
    public async Task CreateBookingAsync_AflysteBookingerTaellerIkkeMedIKapaciteten()
    {
        // Kapacitet 1: en Aflyst booking må ikke "optage" den sidste plads for en ny bruger.
        await using var context = CreateContext();
        var eventId = await SeedEventAsync(context, capacity: 1);
        var annulleretBrugerId = await SeedUserAsync(context, "Annulleret");
        var nyBrugerId = await SeedUserAsync(context, "Ny");

        context.Bookings.Add(new Booking
        {
            EventId = eventId,
            UserId = annulleretBrugerId,
            Status = BookingStatus.Aflyst
        });
        await context.SaveChangesAsync();

        var sut = new BookingService(context);
        var booking = await sut.CreateBookingAsync(eventId, nyBrugerId);

        Assert.Equal(BookingStatus.Booket, booking.Status);
    }

    [Fact]
    public async Task CreateBookingAsync_SammeBrugerBookerSammeEventToGange_KasterConflictException()
    {
        await using var context = CreateContext();
        var eventId = await SeedEventAsync(context, capacity: 5);
        var userId = await SeedUserAsync(context);
        var sut = new BookingService(context);

        await sut.CreateBookingAsync(eventId, userId);

        await Assert.ThrowsAsync<ConflictException>(() => sut.CreateBookingAsync(eventId, userId));
    }

    [Fact]
    public async Task CreateBookingAsync_UkendtEvent_KasterNotFoundException()
    {
        await using var context = CreateContext();
        var sut = new BookingService(context);
        var userId = await SeedUserAsync(context);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.CreateBookingAsync(eventId: 999, userId));
    }

    [Fact]
    public async Task GetBookingsForUserAsync_ReturnererKunDenGivneBrugersEgneBookinger()
    {
        await using var context = CreateContext();
        var eventId = await SeedEventAsync(context, capacity: 5);
        var minId = await SeedUserAsync(context, "Mig");
        var andenId = await SeedUserAsync(context, "AndenBruger");
        var sut = new BookingService(context);

        await sut.CreateBookingAsync(eventId, minId);
        await sut.CreateBookingAsync(eventId, andenId);

        var mineBookinger = await sut.GetBookingsForUserAsync(minId);

        Assert.Single(mineBookinger);
        Assert.Equal(minId, mineBookinger[0].UserId);
    }
}
