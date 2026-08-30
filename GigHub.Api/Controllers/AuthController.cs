using GigHub.Api.Dtos;
using GigHub.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GigHub.Api.Controllers;

/// <summary>Register/login (gang 06). Ingen [Authorize] - det er jo netop her, man BLIVER autentificeret.</summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto, CancellationToken ct)
    {
        // ModelState-validering (fx [Required]/[EmailAddress] på RegisterDto) håndteres
        // automatisk af [ApiController]-attributten - ugyldige requests returnerer 400
        // med en ProblemDetails-valideringsfejl, før denne metode overhovedet kaldes.
        var response = await _authService.RegisterAsync(dto, ct);
        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto, CancellationToken ct)
    {
        var response = await _authService.LoginAsync(dto, ct);
        return Ok(response);
    }
}
