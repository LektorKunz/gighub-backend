using GigHub.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GigHub.Api.Data;

/// <summary>
/// Seed-data, så man har noget at klikke rundt i lige efter <c>dotnet ef database update</c>,
/// uden manuelt at skulle oprette brugere/events via Scalar først. Hører til gang 03's
/// "migration, seed-data" (se endpoint-tabellen i design-brief.md afsnit 4).
///
/// Kaldes fra Program.cs ved opstart og er idempotent - kører kun, hvis Users-tabellen er tom,
/// så den ikke dubleret-sår data ved hver genstart af applikationen.
/// </summary>
public static class DbSeeder
{
    /// <summary>Alle seedede brugere har denne adgangskode - kun til lokal undervisningsbrug.</summary>
    public const string SeedUserPassword = "Password123!";

    public static async Task SeedAsync(GighubDbContext context)
    {
        // Kør migrationer op til nyeste, hvis de ikke allerede er kørt. Underviseren skal
        // stadig selv oprette InitialCreate-migrationen (se README), men denne linje sikrer
        // at databasen er opdateret, næste gang projektet startes efter en ny migration.
        await context.Database.MigrateAsync();

        if (await context.Users.AnyAsync())
        {
            return; // allerede seedet
        }

        var hasher = new PasswordHasher<User>();

        var admin = new User { Name = "Alma Admin", Email = "admin@gighub.dk", PasswordHash = "" };
        admin.PasswordHash = hasher.HashPassword(admin, SeedUserPassword);
        admin.Role = UserRole.Admin;

        var arrangoer1 = new User { Name = "Rasmus Arrangør", Email = "rasmus@gighub.dk", PasswordHash = "" };
        arrangoer1.PasswordHash = hasher.HashPassword(arrangoer1, SeedUserPassword);
        arrangoer1.Role = UserRole.Arrangoer;

        var arrangoer2 = new User { Name = "Sofie Spillested", Email = "sofie@gighub.dk", PasswordHash = "" };
        arrangoer2.PasswordHash = hasher.HashPassword(arrangoer2, SeedUserPassword);
        arrangoer2.Role = UserRole.Arrangoer;

        var deltager1 = new User { Name = "Mikkel Deltager", Email = "mikkel@gighub.dk", PasswordHash = "" };
        deltager1.PasswordHash = hasher.HashPassword(deltager1, SeedUserPassword);
        deltager1.Role = UserRole.Deltager;

        var deltager2 = new User { Name = "Freja Festglad", Email = "freja@gighub.dk", PasswordHash = "" };
        deltager2.PasswordHash = hasher.HashPassword(deltager2, SeedUserPassword);
        deltager2.Role = UserRole.Deltager;

        context.Users.AddRange(admin, arrangoer1, arrangoer2, deltager1, deltager2);
        await context.SaveChangesAsync();

        var koncertIGaar = new Event
        {
            Title = "Campus Rock Night",
            Description = "Lokale bands varmer op til eksamensugen med high-energy rock.",
            Genre = EventGenre.Koncert,
            VenueName = "Auditorium 1",
            Address = "Campusvej 5, 8000 Aarhus C",
            DateTimeUtc = DateTime.UtcNow.AddDays(-3), // overstået - kan anmeldes
            Capacity = 2,
            ArrangoerId = arrangoer1.Id
        };

        var standupINæsteUge = new Event
        {
            Title = "Standup i Kantinen",
            Description = "En times stand-up med tre lokale komikere, gratis kaffe inkluderet.",
            Genre = EventGenre.Standup,
            VenueName = "Kantinen, bygning C",
            Address = "Campusvej 5, 8000 Aarhus C",
            DateTimeUtc = DateTime.UtcNow.AddDays(7),
            Capacity = 40,
            ArrangoerId = arrangoer1.Id
        };

        var festOmEnMaaned = new Event
        {
            Title = "Semesterafslutningsfest",
            Description = "Fest for alle studerende - DJ, bar og fotoboks.",
            Genre = EventGenre.Fest,
            VenueName = "Festsalen",
            Address = "Studenterhus, Campusvej 3, 8000 Aarhus C",
            DateTimeUtc = DateTime.UtcNow.AddDays(30),
            Capacity = 150,
            ArrangoerId = arrangoer2.Id
        };

        var foredrag = new Event
        {
            Title = "Foredrag: Fra studerende til iværksætter",
            Description = "Tidligere studerende fortæller om vejen fra semesterprojekt til eget firma.",
            Genre = EventGenre.Foredrag,
            VenueName = "Auditorium 2",
            Address = "Campusvej 5, 8000 Aarhus C",
            DateTimeUtc = DateTime.UtcNow.AddDays(14),
            Capacity = 80,
            ArrangoerId = arrangoer2.Id
        };

        context.Events.AddRange(koncertIGaar, standupINæsteUge, festOmEnMaaned, foredrag);
        await context.SaveChangesAsync();

        // En overstået, booket deltagelse - så der er noget at anmelde med det samme
        // (demonstrerer forretningsregel 2 uden at skulle vente en uge i virkeligheden).
        context.Bookings.Add(new Booking
        {
            EventId = koncertIGaar.Id,
            UserId = deltager1.Id,
            Status = BookingStatus.Booket
        });

        context.Favorites.Add(new Favorite
        {
            UserId = deltager1.Id,
            EventId = festOmEnMaaned.Id
        });

        await context.SaveChangesAsync();
    }
}
