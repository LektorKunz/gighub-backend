namespace GigHub.Api.Models;

/// <summary>
/// En brugers tilmelding til et event. Unik pr. (EventId, UserId) - håndhævet af et unikt
/// indeks i GighubDbContext, så man ikke kan booke samme event to gange (forretningsregel 3).
/// </summary>
public class Booking
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public int UserId { get; set; }

    /// <summary>Sættes af IBookingService ud fra kapacitetstjek - vælges aldrig af klienten selv.</summary>
    public BookingStatus Status { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // --- Navigation properties -------------------------------------------
    public Event? Event { get; set; }

    public User? User { get; set; }
}
