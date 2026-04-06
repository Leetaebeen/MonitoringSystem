using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;

namespace MonitoringSystem.Frontend.Services.Auth;

public class AuthService
{
    private const string TokenKey = "authToken";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ProtectedSessionStorage _sessionStorage;
    private readonly ILogger<AuthService> _logger;

    public string? Token { get; private set; }
    public string? Username { get; private set; }
    public string? Role { get; private set; }
    public bool IsAuthenticated => Token is not null;

    public event Action? OnAuthStateChanged;

    public AuthService(
        IHttpClientFactory httpClientFactory,
        ProtectedSessionStorage sessionStorage,
        ILogger<AuthService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _sessionStorage = sessionStorage;
        _logger = logger;
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendApi");
            var response = await client.PostAsJsonAsync("api/auth/login", new { username, password });

            if (!response.IsSuccessStatusCode)
                return false;

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (result is null) return false;

            Token = result.Token;
            Username = result.Username;
            Role = result.Role;

            await _sessionStorage.SetAsync(TokenKey, Token);
            OnAuthStateChanged?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "로그인 요청 중 오류 발생");
            return false;
        }
    }

    public async Task TryRestoreFromSessionAsync()
    {
        try
        {
            var result = await _sessionStorage.GetAsync<string>(TokenKey);
            if (!result.Success || string.IsNullOrEmpty(result.Value)) return;

            var token = result.Value;
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            if (jwt.ValidTo < DateTime.UtcNow)
            {
                await LogoutAsync();
                return;
            }

            Token = token;
            Username = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name
                || c.Type == "name" || c.Type == JwtRegisteredClaimNames.Sub)?.Value;
            Role = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role
                || c.Type == "role")?.Value;

            OnAuthStateChanged?.Invoke();
        }
        catch
        {
            // 세션 스토리지 접근 불가(prerender 등) — 무시
        }
    }

    public async Task LogoutAsync()
    {
        Token = null;
        Username = null;
        Role = null;

        try { await _sessionStorage.DeleteAsync(TokenKey); } catch { }
        OnAuthStateChanged?.Invoke();
    }

    private record LoginResponse(string Token, string Username, string Role);
}
