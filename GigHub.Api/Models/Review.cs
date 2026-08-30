namespace GigHub.Api.Models;

/// <summary>
/// En brugers anmeldelse af et event. Kun gyldig at oprette hvis brugeren har en
/// Booket-booking på eventet, OG eventet er overstået - håndhævet i IReviewService,
/// IKKE i denne model eller i databasen (det er domænelogik, ikke datastruktur).
/// Se forretningsregel 2 i design-brief.md.
/// </summary>
public class Review
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public int UserId { get; set; }

    /// <summary>1-5 stjerner. Valideres i ReviewCreateDto med [Range(1,5)].</summary>
    public int Rating { get; set; }

    public required string Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // --- Navigation properties -------------------------------------------
    public Event? Event { get; set; }

    public User? User { get; set; }
}
