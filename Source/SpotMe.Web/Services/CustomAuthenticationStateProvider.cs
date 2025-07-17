using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace SpotMe.Web.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private AuthenticationState authenticationState;

    public CustomAuthenticationStateProvider(CustomAuthenticationService service)
    {
        authenticationState = new AuthenticationState(service.CurrentUser);

        service.UserChanged += (newUser) =>
        {
            authenticationState = new AuthenticationState(newUser);
            NotifyAuthenticationStateChanged(Task.FromResult(authenticationState));
        };

        // Initialize the service to load from cookies if needed
        _ = Task.Run(async () =>
        {
            try
            {
                await service.InitializeAsync();
            }
            catch
            {
                // Ignore initialization errors
            }
        });
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
        Task.FromResult(authenticationState);
} 