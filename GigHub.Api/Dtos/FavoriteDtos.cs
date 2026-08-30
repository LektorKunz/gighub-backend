namespace GigHub.Api.Dtos;

/// <summary>Bruges af GET /api/favorites, så favorit-hjertet i Angular kan vise sin initiale state.</summary>
public record FavoriteDto(int UserId, int EventId, string EventTitle, DateTime CreatedAt);
