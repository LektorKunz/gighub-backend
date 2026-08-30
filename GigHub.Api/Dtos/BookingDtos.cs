using GigHub.Api.Models;

namespace GigHub.Api.Dtos;

/// <summary>
/// Læse-DTO for en booking. Der findes bevidst INGEN "BookingCreateDto" - fra gang 06 hentes
/// UserId udelukkende fra JWT-claims (se ClaimsPrincipalExtensions.GetUserId), og Status
/// bestemmes udelukkende af IBookingService's kapacitetslogik. Klienten sender derfor ikke
/// noget request-body til POST /api/events/{id}/bookings, kun eventId i URL'en.
/// </summary>
public record BookingDto(int Id, int EventId, int UserId, BookingStatus Status, DateTime CreatedAt);
