using GigHub.Api.Common;
using GigHub.Api.Dtos;
using GigHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GigHub.Api.Controllers;

/// <summary>
/// Anmeldelser, nested under events (gang 07). Al forretningslogik (forretningsregel 2 i
/// design-brief.md) ligger i IReviewService, ikke her - controlleren orkestrerer kun HTTP.
/// </summary>
[ApiController]
[Route("api/events/{eventId:int}/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    /// <summary>
    /// GET .../reviews - understøttende læse-endpoint (ikke eksplicit i design-briefens
    /// endpoint-tabel), som Angular's ReviewListComponent (gang 07) bruger til at vise
    /// anmeldelser under et event. Offentligt tilgængeligt, ingen [Authorize].
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReviewDto>>> GetReviews(int eventId, CancellationToken ct)
    {
        var reviews = await _reviewService.GetReviewsForEventAsync(eventId, ct);
        return Ok(reviews);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ReviewDto>> CreateReview(int eventId, ReviewCreateDto dto, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var review = await _reviewService.CreateReviewAsync(eventId, userId, dto, ct);
        return CreatedAtAction(nameof(GetReviews), new { eventId }, review);
    }
}
