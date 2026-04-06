using Microsoft.AspNetCore.Mvc;
using MonitoringSystem.Backend.Services.Auth;

namespace MonitoringSystem.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    // 테스트용 하드코딩 사용자 (실제 운영에서는 DB로 대체)
    private static readonly Dictionary<string, (string Password, string Role)> Users = new()
    {
        ["admin"]    = ("admin123", "Admin"),
        ["operator"] = ("op123",    "Operator"),
        ["viewer"]   = ("view123",  "Viewer")
    };

    private readonly JwtTokenService _jwtTokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(JwtTokenService jwtTokenService, ILogger<AuthController> logger)
    {
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (!Users.TryGetValue(request.Username, out var user) || user.Password != request.Password)
        {
            _logger.LogWarning("로그인 실패. Username={Username}", request.Username);
            return Unauthorized(new { message = "아이디 또는 비밀번호가 올바르지 않습니다." });
        }

        var token = _jwtTokenService.GenerateToken(request.Username, user.Role);

        _logger.LogInformation("로그인 성공. Username={Username}, Role={Role}", request.Username, user.Role);

        return Ok(new LoginResponse
        {
            Token = token,
            Username = request.Username,
            Role = user.Role
        });
    }
}

public record LoginRequest(string Username, string Password);

public record LoginResponse
{
    public string Token { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}
