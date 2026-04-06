using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoringSystem.Backend.Data;
using MonitoringSystem.Backend.Models;
using MonitoringSystem.Backend.Services.Auth;
using System.ComponentModel.DataAnnotations;

namespace MonitoringSystem.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly MonitoringDbContext _db;
    private readonly JwtTokenService _jwtTokenService;
    private readonly IPasswordHasher<AppUser> _passwordHasher;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        MonitoringDbContext db,
        JwtTokenService jwtTokenService,
        IPasswordHasher<AppUser> passwordHasher,
        ILogger<AuthController> logger)
    {
        _db = db;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var username = request.Username.Trim();

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user is null)
        {
            _logger.LogWarning("Login failed. Username={Username}", username);
            return Unauthorized(new { message = "아이디 또는 비밀번호가 올바르지 않습니다." });
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Login failed (wrong password). Username={Username}", username);
            return Unauthorized(new { message = "아이디 또는 비밀번호가 올바르지 않습니다." });
        }

        var token = _jwtTokenService.GenerateToken(user.Username, user.Role);
        _logger.LogInformation("Login succeeded. Username={Username}, Role={Role}", user.Username, user.Role);

        return Ok(new LoginResponse
        {
            Token = token,
            Username = user.Username,
            Role = user.Role
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var username = request.Username.Trim();

        var exists = await _db.Users.AnyAsync(u => u.Username == username);
        if (exists)
        {
            _logger.LogWarning("Registration failed. Duplicate username={Username}", username);
            return Conflict(new { message = "이미 사용 중인 아이디입니다." });
        }

        var user = new AppUser
        {
            Username = username,
            Role = "Operator",
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = _jwtTokenService.GenerateToken(user.Username, user.Role);
        _logger.LogInformation("Registration succeeded. Username={Username}, Role=Operator", user.Username);

        return Ok(new LoginResponse
        {
            Token = token,
            Username = user.Username,
            Role = user.Role
        });
    }
}

public record LoginRequest(
    [Required, MinLength(3)] string Username,
    [Required, MinLength(4)] string Password);

public record RegisterRequest(
    [Required, MinLength(3)] string Username,
    [Required, MinLength(4)] string Password);

public record LoginResponse
{
    public string Token { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}
