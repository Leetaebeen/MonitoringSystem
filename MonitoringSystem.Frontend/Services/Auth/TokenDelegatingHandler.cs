namespace MonitoringSystem.Frontend.Services.Auth;

public class TokenDelegatingHandler : DelegatingHandler
{
    private readonly AuthService _authService;

    public TokenDelegatingHandler(AuthService authService)
    {
        _authService = authService;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_authService.Token))
        {
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authService.Token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
