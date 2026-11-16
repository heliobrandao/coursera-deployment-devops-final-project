using LogiTrack.Dtos;
using LogiTrack.Models;
using LogiTrack.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LogiTrack.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        JwtTokenService jwtTokenService
    )
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterRequest request
    )
    {
        var normalizedEmail = request.Email
            .Trim()
            .ToLowerInvariant();

        var existingUser =
            await _userManager.FindByEmailAsync(
                normalizedEmail
            );

        if (existingUser is not null)
        {
            return Conflict(new
            {
                message =
                    "A user with this email already exists."
            });
        }

        var user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail
        };

        var result = await _userManager.CreateAsync(
            user,
            request.Password
        );

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = "User registration failed.",
                errors = result.Errors.Select(
                    error => error.Description
                )
            });
        }

        await _userManager.AddToRoleAsync(user, "User");

        return StatusCode(
            StatusCodes.Status201Created,
            new
            {
                message = "User registered successfully.",
                userId = user.Id,
                email = user.Email,
                role = "User"
            }
        );
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginRequest request
    )
    {
        var normalizedEmail = request.Email
            .Trim()
            .ToLowerInvariant();

        var user =
            await _userManager.FindByEmailAsync(
                normalizedEmail
            );

        if (user is null)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        var result =
            await _signInManager.CheckPasswordSignInAsync(
                user,
                request.Password,
                lockoutOnFailure: true
            );

        if (!result.Succeeded)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        var token =
            await _jwtTokenService.GenerateTokenAsync(user);

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new
        {
            tokenType = "Bearer",
            accessToken = token,
            expiresInMinutes = 60,
            roles
        });
    }
}