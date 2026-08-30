using GigHub.Api.Common.Exceptions;
using GigHub.Api.Data;
using GigHub.Api.Dtos;
using GigHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GigHub.Api.Services;

/// <summary>
/// FACIT-implementation af forretningsregel 2. Den pædagogiske pointe i gang 07 er at lade
/// de studerende først bygge POST /api/events/{id}/reviews UDEN disse tjek (så "hvem som helst
/// kan anmelde hvad som helst" bliver synligt som et problem), og først derefter rette det til
/// denne version. Se design-brief.md afsnit 4.
/// </summary>
public class ReviewService : IReviewService
{
    private readonly GighubDbContext _context;

    public ReviewService(GighubDbContext context)
    {
        _context = context;
    }

    public async Task<ReviewDto> CreateReviewAsync(int eventId, int userId, ReviewCreateDto dto, CancellationToken ct = default)
    {
        var gigEvent = await _context.Events.FindAsync(new object[] { eventId }, ct)
            ?? throw new NotFoundException($"Event med id {eventId} findes ikke.");

        if (gigEvent.DateTimeUtc >= DateTime.UtcNow)
        {
            throw new ConflictException("Du kan først anmelde et event, når det er overstået.");
        }

        var hasAttended = await _context.Bookings.AnyAsync(
            b => b.EventId == eventId && b.UserId == userId && b.Status == BookingStatus.Booket,
            ct);

        if (!hasAttended)
        {
            throw new ForbiddenException("Du kan kun anmelde events, du selv har været booket til.");
        }

        var alreadyReviewed = await _context.Reviews.AnyAsync(
            r => r.EventId == eventId && r.UserId == userId, ct);

        if (alreadyReviewed)
        {
            throw new ConflictException("Du har allerede anmeldt dette event.");
        }

        var review = new Review
        {
            EventId = eventId,
            UserId = userId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            CreatedAt = DateTime.UtcNow
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync(ct);

        var user = await _context.Users.FindAsync(new object[] { userId }, ct);

        return new ReviewDto(review.Id, review.EventId, review.UserId, user?.Name ?? string.Empty,
            review.Rating, review.Comment, review.CreatedAt);
    }

    public async Task<IReadOnlyList<ReviewDto>> GetReviewsForEventAsync(int eventId, CancellationToken ct = default)
    {
        // Ingen eksplicit .Include(r => r.User) her - EF Core kan selv oversætte adgangen til
        // r.User.Name inde i Select-projektionen nedenfor til en JOIN. Et eksplicit Include
        // kombineret med en projektion, der ikke returnerer selve entiteten, giver kun en
        // (harmløs) log-advarsel om, at Include'et er overflødigt.
        return await _context.Reviews
            .Where(r => r.EventId == eventId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewDto(r.Id, r.EventId, r.UserId, r.User!.Name, r.Rating, r.Comment, r.CreatedAt))
            .ToListAsync(ct);
    }
}
