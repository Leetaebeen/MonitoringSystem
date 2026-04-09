using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using MonitoringSystem.Frontend.Services.Auth;

namespace MonitoringSystem.Frontend.Components.Features.Auth.Pages;

public partial class Login
{
    [Inject] private AuthService AuthService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private readonly LoginModel _loginModel = new();
    private readonly RegisterModel _registerModel = new();
    private string? _errorMessage;
    private string? _successMessage;
    private bool _isLoading;
    private bool _isRegisterMode;

    private CredentialsModel CurrentModel => _isRegisterMode ? _registerModel : _loginModel;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        await AuthService.TryRestoreFromSessionAsync();
        if (AuthService.IsAuthenticated)
            Navigation.NavigateTo("/", forceLoad: false);
    }

    private Task ShowLoginMode()
    {
        _isRegisterMode = false;
        _errorMessage = null;
        _successMessage = null;
        return Task.CompletedTask;
    }

    private Task ShowRegisterMode()
    {
        _isRegisterMode = true;
        _errorMessage = null;
        _successMessage = null;
        return Task.CompletedTask;
    }

    private string GetModeClass(bool registerMode) => _isRegisterMode == registerMode ? "active" : string.Empty;

    private Task HandleSubmitAsync() => _isRegisterMode ? HandleRegisterAsync() : HandleLoginAsync();

    private async Task HandleLoginAsync()
    {
        _isLoading = true;
        _errorMessage = null;
        _successMessage = null;

        var success = await AuthService.LoginAsync(_loginModel.Username, _loginModel.Password);

        if (success)
        {
            Navigation.NavigateTo("/", forceLoad: false);
            return;
        }

        _errorMessage = "아이디 또는 비밀번호가 올바르지 않습니다.";
        _isLoading = false;
    }

    private async Task HandleRegisterAsync()
    {
        _isLoading = true;
        _errorMessage = null;
        _successMessage = null;

        var result = await AuthService.RegisterAsync(_registerModel.Username, _registerModel.Password);

        if (result.Success)
        {
            _successMessage = "회원가입이 완료되었습니다.";
            Navigation.NavigateTo("/", forceLoad: false);
            return;
        }

        _errorMessage = result.ErrorMessage ?? "회원가입에 실패했습니다.";
        _isLoading = false;
    }

    private class CredentialsModel
    {
        [Required(ErrorMessage = "아이디를 입력해주세요.")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "아이디는 3자 이상 20자 이하로 입력해주세요.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "비밀번호를 입력해주세요.")]
        [StringLength(50, MinimumLength = 4, ErrorMessage = "비밀번호는 4자 이상 입력해주세요.")]
        public string Password { get; set; } = string.Empty;
    }

    private sealed class LoginModel : CredentialsModel;

    private sealed class RegisterModel : CredentialsModel, IValidatableObject
    {
        [Required(ErrorMessage = "비밀번호 확인을 입력해주세요.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
            {
                yield return new ValidationResult(
                    "비밀번호 확인이 일치하지 않습니다.",
                    [nameof(ConfirmPassword)]);
            }
        }
    }
}
