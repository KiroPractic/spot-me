using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace SpotMe.Web.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly CustomAuthenticationService _service;
    private readonly TaskCompletionSource<AuthenticationState> _authenticationState;

    public CustomAuthenticationStateProvider(CustomAuthenticationService service)
    {
        _service = service;
        _authenticationState = new TaskCompletionSource<AuthenticationState>();

        service.UserChanged += (newUser) =>
        {
            var newAuthState = new AuthenticationState(newUser);
            if (_authenticationState.Task.IsCompleted)
            {
                NotifyAuthenticationStateChanged(Task.FromResult(newAuthState));
            }
            else
            {
                _authenticationState.SetResult(newAuthState);
            }
        };

        // Initialize immediately and synchronously if possible
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            // Try to initialize the service
            await _service.InitializeAsync();
            
            // Set the initial authentication state
            if (!_authenticationState.Task.IsCompleted)
            {
                _authenticationState.SetResult(new AuthenticationState(_service.CurrentUser));
            }
        }
        catch
        {
            // If initialization fails, set anonymous user
            if (!_authenticationState.Task.IsCompleted)
            {
                _authenticationState.SetResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
            }
        }
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // Wait for initialization to complete
        return await _authenticationState.Task;
    }
} 