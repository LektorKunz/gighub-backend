namespace GigHub.Api.Models;

/// <summary>
/// Et event (koncert, fest, standup, foredrag, ...) i GigHub. Ejes af en arrangør
/// (<see cref="ArrangoerId"/>), og har bookinger, anmeldelser og favoritmarkeringer.
/// </summary>
public class Event
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public required string Description { get; set; }

    public EventGenre Genre { get; set; }

    public required string VenueName { get; set; }

    public required string Address { get; set; }

    /// <summary>Tidspunktet, eventet afholdes, i UTC - konverteres til lokal tid i Angular.</summary>
    public DateTime DateTimeUtc { get; set; }

    /// <summary>Maks. antal Booket-bookinger, før nye bookinger går på venteliste.</summary>
    public int Capacity { get; set; }

    /// <summary>Sti til uploadet billede (fx "/uploads/events/12-abc.jpg"), sat via POST .../image.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>FK til den bruger, der har oprettet eventet.</summary>
    public int ArrangoerId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // --- Navigation properties -------------------------------------------

    /// <summary>Nullable, fordi EF Core ikke sætter den, før man eksplicit har lavet Include(e =&gt; e.Arrangoer).</summary>
    public User? Arrangoer { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
}
