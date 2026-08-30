namespace GigHub.Api.Models;

/// <summary>
/// En bruger af GigHub. Kan være deltager, arrangør eller admin, se <see cref="UserRole"/>.
/// OBS: password gemmes ALDRIG i klartekst - kun <see cref="PasswordHash"/>, produceret af
/// <see cref="Microsoft.AspNetCore.Identity.PasswordHasher{TUser}"/> i AuthService.
/// </summary>
public class User
{
    public int Id { get; set; }

    public required string Name { get; set; }

    /// <summary>Unik pr. bruger - håndhæves af et unikt indeks, se GighubDbContext.</summary>
    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public UserRole Role { get; set; } = UserRole.Deltager;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // --- Navigation properties -------------------------------------------
    // Initialiseret til tomme lister, så man aldrig risikerer en NullReferenceException,
    // hvis man tilgår fx bruger.Bookings, før EF Core har hentet dem via Include().

    /// <summary>Events, denne bruger er arrangør for (kun relevant hvis Role = Arrangoer/Admin).</summary>
    public ICollection<Event> ArrangeredEvents { get; set; } = new List<Event>();

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
}
