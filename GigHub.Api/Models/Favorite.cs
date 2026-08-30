namespace GigHub.Api.Models;

/// <summary>
/// Kobletabel for mange-til-mange-relationen User &lt;-&gt; Event ("hjerte"-markering af events).
/// Har IKKE sin egen Id - den sammensatte nøgle (UserId, EventId) ER identiteten
/// (en bruger kan kun favoritmarkere et givent event én gang). Se GighubDbContext.OnModelCreating.
/// </summary>
public class Favorite
{
    public int UserId { get; set; }

    public int EventId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // --- Navigation properties -------------------------------------------
    public User? User { get; set; }

    public Event? Event { get; set; }
}
