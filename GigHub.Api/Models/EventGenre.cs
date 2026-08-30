namespace GigHub.Api.Models;

/// <summary>Genre/kategori for et event. Bruges til filtrering i GET /api/events?genre=.</summary>
public enum EventGenre
{
    Koncert,
    Fest,
    Standup,
    Foredrag,
    Andet
}
