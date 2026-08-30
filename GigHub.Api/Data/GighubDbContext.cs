using GigHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GigHub.Api.Data;

/// <summary>
/// EF Core-databasekontekst for GigHub. Svarer til "forbindelsen + tabellerne" fra SQL-verdenen,
/// men modelleres Code First ud fra klasserne i Models/ - se bro-tabellen "Rå SQL → EF Core" i
/// design-brief.md afsnit 5.
/// </summary>
public class GighubDbContext : DbContext
{
    public GighubDbContext(DbContextOptions<GighubDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Event> Events => Set<Event>();

    public DbSet<Booking> Bookings => Set<Booking>();

    public DbSet<Review> Reviews => Set<Review>();

    public DbSet<Favorite> Favorites => Set<Favorite>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- User -----------------------------------------------------------
        modelBuilder.Entity<User>(entity =>
        {
            // Email skal være unik - ellers kunne to brugere registrere sig med samme email,
            // og login-opslaget i AuthService ville være tvetydigt.
            entity.HasIndex(u => u.Email).IsUnique();

            // Gem enum som streng ("Deltager") i stedet for int (0) - læsbart direkte i
            // DB Browser for SQLite, og robust over for at rækkefølgen i enum'en ændres senere.
            entity.Property(u => u.Role)
                .HasConversion<string>()
                .HasMaxLength(20);
        });

        // --- Event ------------------------------------------------------------
        modelBuilder.Entity<Event>(entity =>
        {
            entity.Property(e => e.Genre)
                .HasConversion<string>()
                .HasMaxLength(20);

            // En arrangør kan have mange events, men et event har præcis én arrangør.
            // Restrict (ikke Cascade): sletter man en bruger, skal deres events IKKE
            // automatisk forsvinde - det ville kunne slette et event, andre har booket.
            entity.HasOne(e => e.Arrangoer)
                .WithMany(u => u.ArrangeredEvents)
                .HasForeignKey(e => e.ArrangoerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- Booking ------------------------------------------------------------
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.Property(b => b.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            // Forretningsregel 3: en bruger kan ikke booke samme event to gange.
            // Dette er den "ægte" beskyttelse - IBookingService's eget tjek (AnyAsync,
            // se BookingService.CreateBookingAsync) er UX-lag, der giver en pæn 409 i stedet
            // for at lade denne DB-constraint fejle råt.
            entity.HasIndex(b => new { b.EventId, b.UserId }).IsUnique();

            // Cascade: slettes eventet, giver bookinger på det ikke mening at beholde.
            entity.HasOne(b => b.Event)
                .WithMany(e => e.Bookings)
                .HasForeignKey(b => b.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Restrict: slettes en bruger, skal deres bookinger ikke bare forsvinde stille -
            // det ville forvrænge kapacitetstællingen for andre. (Vi har ikke bygget
            // bruger-sletning i dette forløb, men reglen er sat rigtigt fra start.)
            entity.HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- Review ------------------------------------------------------------
        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasOne(r => r.Event)
                .WithMany(e => e.Reviews)
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- Favorite (sammensat nøgle, mange-til-mange User <-> Event) ---------
        modelBuilder.Entity<Favorite>(entity =>
        {
            // Ingen selvstændig Id - nøglen ER kombinationen (UserId, EventId), som SQL-holdet
            // kender fra en klassisk kobletabel med sammensat primærnøgle.
            entity.HasKey(f => new { f.UserId, f.EventId });

            entity.HasOne(f => f.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(f => f.Event)
                .WithMany(e => e.Favorites)
                .HasForeignKey(f => f.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
