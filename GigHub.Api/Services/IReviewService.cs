using GigHub.Api.Dtos;

namespace GigHub.Api.Services;

/// <summary>
/// Anmeldelser med forretningsregel 2 (design-brief.md): en bruger må kun anmelde et event,
/// de rent faktisk har været Booket til, og først efter eventet er overstået.
/// </summary>
public interface IReviewService
{
    /// <exception cref="GigHub.Api.Common.Exceptions.NotFoundException">Eventet findes ikke.</exception>
    /// <exception cref="GigHub.Api.Common.Exceptions.ForbiddenException">Brugeren har ikke en Booket-booking på eventet.</exception>
    /// <exception cref="GigHub.Api.Common.Exceptions.ConflictException">Eventet er ikke overstået endnu, eller er allerede anmeldt af brugeren.</exception>
    Task<ReviewDto> CreateReviewAsync(int eventId, int userId, ReviewCreateDto dto, CancellationToken ct = default);

    Task<IReadOnlyList<ReviewDto>> GetReviewsForEventAsync(int eventId, CancellationToken ct = default);
}
