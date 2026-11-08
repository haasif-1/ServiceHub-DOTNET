using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ServiceHub.Web.Models;
using ServiceHub.Web.Models.ViewModels;
using ServiceHub.Web.Services;

namespace ServiceHub.Web.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly JwtTokenService _jwtTokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        JwtTokenService jwtTokenService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    [HttpPost("token")]
    [AllowAnonymous]
    public async Task<IActionResult> Token([FromBody] JwtTokenRequest request)
    {
        if (!_jwtTokenService.IsConfigured)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "JWT is not configured. Set Jwt:Key (at least 32 characters) in user secrets or environment variables."
            });
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized(new { error = "Invalid email or password." });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            return StatusCode(StatusCodes.Status423Locked, new
            {
                error = "This account is locked after too many failed attempts. Try again in 15 minutes."
            });
        }

        if (!result.Succeeded)
        {
            return Unauthorized(new { error = "Invalid email or password." });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwtTokenService.CreateToken(user, roles);

        _logger.LogInformation("Issued a JWT for user {UserId}.", user.Id);

        return Ok(new
        {
            token,
            tokenType = "Bearer",
            expiresInMinutes = 60,
            email = user.Email,
            roles
        });
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                ?? User.FindFirst("email")?.Value,
            name = User.Identity?.Name,
            roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(claim => claim.Value)
        });
    }
}
