using System.ComponentModel.DataAnnotations;

namespace GigHub.Api.Dtos;

public record ReviewCreateDto(
    [property: Range(1, 5)] int Rating,
    [property: Required, MaxLength(1000)] string Comment);

public record ReviewDto(
    int Id,
    int EventId,
    int UserId,
    string UserName,
    int Rating,
    string Comment,
    DateTime CreatedAt);
