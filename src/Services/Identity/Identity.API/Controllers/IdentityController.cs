using Identity.API.DTOs;
using Identity.API.Entities;
using Identity.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.Auth;
using Shared.Common.DTOs;

namespace Identity.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IdentityController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly ILogger<IdentityController> _logger;

    public IdentityController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ITokenService tokenService,
        ILogger<IdentityController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _logger = logger;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("Registration attempt with existing email {Email}", request.Email);
            return BadRequest(ApiResponse<AuthResponse>.Fail("A user with this email already exists."));
        }

        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            _logger.LogWarning("Registration failed for {Email}: {Errors}", request.Email, errors);
            return BadRequest(ApiResponse<AuthResponse>.Fail(errors));
        }

        var role = AppRoles.User;
        if (!string.IsNullOrEmpty(request.Role) && AppRoles.All.Contains(request.Role))
        {
            role = request.Role;
        }

        await _userManager.AddToRoleAsync(user, role);

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id);

        var response = new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresIn = 15 * 60, // 15 minutes in seconds
            UserId = user.Id,
            Email = user.Email!,
            FullName = user.FullName,
            Roles = roles
        };

        _logger.LogInformation("User registered successfully: {Email} with role {Role}", request.Email, role);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<AuthResponse>.Success(response, "Registration successful"));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            _logger.LogWarning("Login attempt for non-existent email {Email}", request.Email);
            return Unauthorized(ApiResponse<AuthResponse>.Fail("Invalid email or password."));
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed login for {Email} (locked={Locked})", request.Email, result.IsLockedOut);
            return Unauthorized(ApiResponse<AuthResponse>.Fail("Invalid email or password."));
        }

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id);

        var response = new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresIn = 15 * 60,
            UserId = user.Id,
            Email = user.Email!,
            FullName = user.FullName,
            Roles = roles
        };

        _logger.LogInformation("User logged in: {Email}", request.Email);
        return Ok(ApiResponse<AuthResponse>.Success(response, "Login successful"));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh([FromBody] RefreshRequest request)
    {
        var newRefreshToken = await _tokenService.RotateRefreshTokenAsync(request.RefreshToken);
        if (newRefreshToken == null)
        {
            return Unauthorized(ApiResponse<AuthResponse>.Fail("Invalid or expired refresh token."));
        }

        var user = await _userManager.FindByIdAsync(newRefreshToken.UserId);
        if (user == null)
        {
            return Unauthorized(ApiResponse<AuthResponse>.Fail("User not found."));
        }

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);

        var response = new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken.Token,
            ExpiresIn = 15 * 60,
            UserId = user.Id,
            Email = user.Email!,
            FullName = user.FullName,
            Roles = roles
        };

        _logger.LogInformation("Token refreshed for user {UserId}", user.Id);
        return Ok(ApiResponse<AuthResponse>.Success(response, "Token refreshed successfully"));
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> Me()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                     ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<object>.Fail("User not found in token."));

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(ApiResponse<object>.Fail("User not found."));

        var roles = await _userManager.GetRolesAsync(user);

        var profile = new
        {
            user.Id,
            user.Email,
            user.FullName,
            user.CreatedAt,
            Roles = roles
        };

        return Ok(ApiResponse<object>.Success(profile));
    }
}
