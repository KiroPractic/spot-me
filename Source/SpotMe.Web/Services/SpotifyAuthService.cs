using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using System.Text;
using System.Text.Json;

namespace SpotMe.Web.Services;

public class SpotifyAuthService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly HttpClient _httpClient;
    private string? _clientId;
    private string? _redirectUri;
    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    // Spotify requires these scopes to use the Web Playback SDK
    private const string _requiredScopes = "streaming user-read-email user-read-private user-read-playback-state user-modify-playback-state";

    public SpotifyAuthService(IJSRuntime jsRuntime, HttpClient httpClient)
    {
        _jsRuntime = jsRuntime;
        _httpClient = httpClient;
    }

    public void Configure(string clientId, string redirectUri)
    {
        _clientId = clientId;
        _redirectUri = redirectUri;
    }

    public string GenerateAuthUrl()
    {
        if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_redirectUri))
        {
            throw new InvalidOperationException("Spotify client ID and redirect URI must be configured before generating auth URL");
        }

        // Generate a random state value for security
        var state = Guid.NewGuid().ToString();
        
        // Store the state in local storage
        _jsRuntime.InvokeVoidAsync("localStorage.setItem", "spotify_auth_state", state);

        // Build the Spotify authorization URL
        var queryParams = new Dictionary<string, string>
        {
            { "client_id", _clientId },
            { "response_type", "token" },
            { "redirect_uri", _redirectUri },
            { "scope", _requiredScopes },
            { "state", state },
            { "show_dialog", "true" }
        };

        return QueryHelpers.AddQueryString("https://accounts.spotify.com/authorize", queryParams);
    }

    public async Task<bool> HandleRedirectAsync()
    {
        try
        {
            // Get the URL fragment from the browser
            var fragment = await _jsRuntime.InvokeAsync<string>("window.location.hash");
            
            if (string.IsNullOrEmpty(fragment) || !fragment.StartsWith("#"))
            {
                return false;
            }

            // Extract the token data from the fragment
            var fragmentParams = fragment.Substring(1)
                .Split('&')
                .Select(param => param.Split('='))
                .ToDictionary(param => param[0], param => param[1]);

            // Verify the state to prevent CSRF attacks
            var storedState = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "spotify_auth_state");
            var returnedState = fragmentParams.GetValueOrDefault("state");

            if (storedState != returnedState)
            {
                throw new InvalidOperationException("State mismatch - possible CSRF attack");
            }

            // Clear the state from storage
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "spotify_auth_state");

            // Store the access token and expiry time
            if (fragmentParams.TryGetValue("access_token", out var token))
            {
                _accessToken = token;
                
                if (fragmentParams.TryGetValue("expires_in", out var expiresIn) && int.TryParse(expiresIn, out var seconds))
                {
                    _tokenExpiry = DateTime.UtcNow.AddSeconds(seconds);
                }
                
                // Clear the fragment from the URL to avoid exposing the token
                await _jsRuntime.InvokeVoidAsync("history.replaceState", null, "", ".");
                
                return true;
            }
            
            return false;
        }
        catch (Exception)
        {
            // Clear any partial authentication state
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "spotify_auth_state");
            return false;
        }
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        // If we have a valid token, return it
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
        {
            return _accessToken;
        }

        // Try to get the token from local storage
        var storedToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "spotify_access_token");
        var storedExpiryStr = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "spotify_token_expiry");

        if (!string.IsNullOrEmpty(storedToken) && !string.IsNullOrEmpty(storedExpiryStr))
        {
            if (DateTime.TryParse(storedExpiryStr, out var storedExpiry) && DateTime.UtcNow < storedExpiry)
            {
                _accessToken = storedToken;
                _tokenExpiry = storedExpiry;
                return _accessToken;
            }
        }

        // We need a new token via OAuth flow
        return null;
    }

    public async Task StoreAccessTokenAsync(string accessToken, int expiresIn)
    {
        _accessToken = accessToken;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn);

        // Store in local storage
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "spotify_access_token", accessToken);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "spotify_token_expiry", _tokenExpiry.ToString("o"));
    }

    public async Task ClearAccessTokenAsync()
    {
        _accessToken = null;
        _tokenExpiry = DateTime.MinValue;

        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "spotify_access_token");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "spotify_token_expiry");
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry;
}