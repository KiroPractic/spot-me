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

    public async Task<string> GenerateAuthUrlAsync()
    {
        if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_redirectUri))
        {
            throw new InvalidOperationException("Spotify client ID and redirect URI must be configured before generating auth URL");
        }

        // Generate a random state value for security
        var state = Guid.NewGuid().ToString();
        
        // Store the state in local storage
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "spotify_auth_state", state);

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
    
    // Keep the old method for backward compatibility, but it will use a fire-and-forget approach
    public string GenerateAuthUrl()
    {
        // Generate a random state value for security
        var state = Guid.NewGuid().ToString();
        
        // Fire and forget - not ideal but keeps the existing API
        _ = _jsRuntime.InvokeVoidAsync("localStorage.setItem", "spotify_auth_state", state);

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
            await _jsRuntime.InvokeVoidAsync("console.log", $"Hash fragment: {fragment}");
            
            if (string.IsNullOrEmpty(fragment) || !fragment.StartsWith("#"))
            {
                await _jsRuntime.InvokeVoidAsync("console.log", "No hash fragment found in URL");
                return false;
            }

            // Extract the token data from the fragment - log raw fragment for debugging
            await _jsRuntime.InvokeVoidAsync("console.log", "Raw fragment:", fragment);
            
            // Use a more robust parsing approach
            var fragmentParams = new Dictionary<string, string>();
            var pairs = fragment.Substring(1).Split('&');
            
            foreach (var pair in pairs)
            {
                var keyValue = pair.Split('=', 2);
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0];
                    var value = System.Web.HttpUtility.UrlDecode(keyValue[1]);
                    fragmentParams[key] = value;
                }
            }
            
            await _jsRuntime.InvokeVoidAsync("console.log", "Parsed fragment parameters:", fragmentParams);
            await _jsRuntime.InvokeVoidAsync("console.log", $"Access token found: {fragmentParams.ContainsKey("access_token")}");
            
            if (fragmentParams.ContainsKey("access_token")) {
                await _jsRuntime.InvokeVoidAsync("console.log", $"Access token starts with: {fragmentParams["access_token"].Substring(0, Math.Min(10, fragmentParams["access_token"].Length))}...");
            }

            // Verify the state to prevent CSRF attacks
            var storedState = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "spotify_auth_state");
            var returnedState = fragmentParams.GetValueOrDefault("state");
            
            await _jsRuntime.InvokeVoidAsync("console.log", $"Stored state: {storedState}, Returned state: {returnedState}");

            if (storedState != returnedState)
            {
                await _jsRuntime.InvokeVoidAsync("console.log", "State mismatch - possible CSRF attack");
                throw new InvalidOperationException("State mismatch - possible CSRF attack");
            }

            // Clear the state from storage
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "spotify_auth_state");

            // Store the access token and expiry time
            if (fragmentParams.TryGetValue("access_token", out var token))
            {
                _accessToken = token;
                await _jsRuntime.InvokeVoidAsync("console.log", "Access token received");
                
                if (fragmentParams.TryGetValue("expires_in", out var expiresIn) && int.TryParse(expiresIn, out var seconds))
                {
                    _tokenExpiry = DateTime.UtcNow.AddSeconds(seconds);
                    await _jsRuntime.InvokeVoidAsync("console.log", $"Token expires in {seconds} seconds");
                    
                    // Store in local storage for persistence
                    await StoreAccessTokenAsync(token, seconds);
                }
                
                // Clear the fragment from the URL to avoid exposing the token
                await _jsRuntime.InvokeVoidAsync("history.replaceState", null, "", "/callback");
                
                return true;
            }
            else
            {
                await _jsRuntime.InvokeVoidAsync("console.log", "No access_token found in fragment");
            }
            
            return false;
        }
        catch (Exception ex)
        {
            // Log the error to console
            await _jsRuntime.InvokeVoidAsync("console.error", $"Error in HandleRedirectAsync: {ex.Message}");
            
            // Clear any partial authentication state
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "spotify_auth_state");
            return false;
        }
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            // If we have a valid token, return it
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
            {
                return _accessToken;
            }

            // Try to get the token from local storage - handle prerendering case
            try
            {
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
            }
            catch (InvalidOperationException)
            {
                // This happens during prerendering - just return null
                // We'll check again in OnAfterRenderAsync
                return null;
            }

            // We need a new token via OAuth flow
            return null;
        }
        catch (Exception ex)
        {
            // Log exception but don't crash
            Console.Error.WriteLine($"Error getting access token: {ex.Message}");
            return null;
        }
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