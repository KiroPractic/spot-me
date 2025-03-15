using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SpotMe.Web.Services;

public class SpotifyService
{
    // Event for authentication state changes
    public delegate void AuthStateChangedEventHandler(bool isAuthenticated);
    public event AuthStateChangedEventHandler? OnAuthStateChanged;
    
    private readonly IJSRuntime _jsRuntime;
    private readonly HttpClient _httpClient;
    private string? _clientId;
    private string? _redirectUri;
    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private SpotifyUserProfile? _cachedProfile;
    private bool _previousAuthState = false;

    // Spotify requires these scopes to use the Web Playback SDK and access library
    private const string _requiredScopes = "streaming user-read-email user-read-private user-read-playback-state user-modify-playback-state user-library-read user-follow-read playlist-read-private playlist-read-collaborative user-read-recently-played";
    
    // Properties to expose for debugging
    public string? ClientId => _clientId;
    public string RequestedScopes => _requiredScopes;

    public SpotifyService(IJSRuntime jsRuntime, HttpClient httpClient)
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
        var queryParams = new Dictionary<string, string?>
        {
            { "client_id", _clientId! },
            { "response_type", "token" },
            { "redirect_uri", _redirectUri! },
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
        var queryParams = new Dictionary<string, string?>
        {
            { "client_id", _clientId! },
            { "response_type", "token" },
            { "redirect_uri", _redirectUri! },
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
            // If we have a valid token in memory, return it
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
            {
                // Check if auth state changed
                bool currentAuthState = true;
                if (currentAuthState != _previousAuthState)
                {
                    _previousAuthState = currentAuthState;
                    OnAuthStateChanged?.Invoke(currentAuthState);
                }
                
                return _accessToken;
            }

            try
            {
                // Use a try/catch block to handle prerendering
                var storedToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "spotify_access_token");
                var storedExpiryStr = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "spotify_token_expiry");

                if (!string.IsNullOrEmpty(storedToken) && !string.IsNullOrEmpty(storedExpiryStr))
                {
                    if (DateTime.TryParse(storedExpiryStr, out var storedExpiry))
                    {
                        if (DateTime.UtcNow < storedExpiry)
                        {
                            _accessToken = storedToken;
                            _tokenExpiry = storedExpiry;
                            
                            // Fire auth state change if needed
                            bool currentAuthState = true;
                            if (currentAuthState != _previousAuthState)
                            {
                                _previousAuthState = currentAuthState;
                                OnAuthStateChanged?.Invoke(currentAuthState);
                            }
                            
                            return _accessToken;
                        }
                    }
                }
                
                // Auth state is false
                bool newAuthState = false;
                if (newAuthState != _previousAuthState)
                {
                    _previousAuthState = newAuthState;
                    OnAuthStateChanged?.Invoke(newAuthState);
                }
            }
            catch (InvalidOperationException)
            {
                // This happens during prerendering - just return null
                // We'll check again in OnAfterRenderAsync
                return null;
            }
            catch (Exception)
            {
                // Silently handle other exceptions
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
        bool wasAuthenticated = IsAuthenticated;
        
        _accessToken = accessToken;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn);

        try
        {
            // Add debug info
            await _jsRuntime.InvokeVoidAsync("console.log", $"Storing token in localStorage (expires in {expiresIn}s)");
            
            // Store in local storage
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "spotify_access_token", accessToken);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "spotify_token_expiry", _tokenExpiry.ToString("o"));
            
            // Verify it was stored correctly
            var verifyToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "spotify_access_token");
            var verifyExpiry = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "spotify_token_expiry");
            
            await _jsRuntime.InvokeVoidAsync("console.log", $"Verified localStorage: token exists = {!string.IsNullOrEmpty(verifyToken)}, expiry exists = {!string.IsNullOrEmpty(verifyExpiry)}");
            
            // Update IsAuthenticated property
            await _jsRuntime.InvokeVoidAsync("console.log", $"Updated authentication state: {IsAuthenticated}");
            
            // Fire auth state change if needed
            if (!wasAuthenticated && IsAuthenticated)
            {
                _previousAuthState = true;
                OnAuthStateChanged?.Invoke(true);
                await _jsRuntime.InvokeVoidAsync("console.log", "Fired authentication state change event (now authenticated)");
            }
        }
        catch (Exception ex)
        {
            await _jsRuntime.InvokeVoidAsync("console.error", $"Error storing token: {ex.Message}");
        }
    }

    public async Task ClearAccessTokenAsync()
    {
        bool wasAuthenticated = IsAuthenticated;
        
        _accessToken = null;
        _tokenExpiry = DateTime.MinValue;
        _cachedProfile = null;

        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "spotify_access_token");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "spotify_token_expiry");
        
        // Fire auth state change if needed
        if (wasAuthenticated)
        {
            _previousAuthState = false;
            OnAuthStateChanged?.Invoke(false);
            await _jsRuntime.InvokeVoidAsync("console.log", "Fired authentication state change event (now logged out)");
        }
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry;
    
    public async Task<SpotifyUserProfile?> GetUserProfileAsync(bool forceRefresh = false)
    {
        // Return cached profile if available and not forcing refresh
        if (!forceRefresh && _cachedProfile != null)
        {
            Console.WriteLine($"GetUserProfileAsync: Using cached profile - {_cachedProfile.DisplayName}");
            return _cachedProfile;
        }
        
        var token = await GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("GetUserProfileAsync: No token available");
            return null;
        }
        
        try 
        {
            // Create a new request to the Spotify API
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            Console.WriteLine("GetUserProfileAsync: Sending request to Spotify API");
            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                // Configure deserialization options
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                var profile = JsonSerializer.Deserialize<SpotifyUserProfile>(content, options);
                
                if (profile != null)
                {
                    Console.WriteLine($"GetUserProfileAsync: Retrieved profile - DisplayName: '{profile.DisplayName}'");
                    // Cache the profile
                    _cachedProfile = profile;
                }
                
                return profile;
            }
            
            // Handle error cases
            Console.Error.WriteLine($"Failed to get user profile: {response.StatusCode}");
            return null;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error getting user profile: {ex.Message}");
            return null;
        }
    }
    
    // Get user's saved tracks (liked songs) count
    public async Task<int> GetSavedTracksCountAsync()
    {
        var token = await GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            return 0;
        }
        
        try 
        {
            // Log access token for debugging (truncated for security)
            if (token.Length > 10)
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"Using token starting with: {token.Substring(0, 10)}...");
            }
            
            // We only need the total count, so limit=1 is sufficient
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me/tracks?limit=1");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            // Log that we're making the request
            await _jsRuntime.InvokeVoidAsync("console.log", "Requesting saved tracks from Spotify API...");
            
            var response = await _httpClient.SendAsync(request);
            
            // Log the response status
            await _jsRuntime.InvokeVoidAsync("console.log", $"Saved tracks response status: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var paginatedResponse = JsonSerializer.Deserialize<PaginatedResponse>(content, options);
                
                await _jsRuntime.InvokeVoidAsync("console.log", $"Retrieved saved tracks count: {paginatedResponse?.Total ?? 0}");
                return paginatedResponse?.Total ?? 0;
            }
            
            // More detailed error logging
            var errorContent = await response.Content.ReadAsStringAsync();
            await _jsRuntime.InvokeVoidAsync("console.error", $"Failed to get saved tracks: {response.StatusCode}");
            await _jsRuntime.InvokeVoidAsync("console.error", $"Error details: {errorContent}");
            
            return 0;
        }
        catch (Exception ex)
        {
            await _jsRuntime.InvokeVoidAsync("console.error", $"Error getting saved tracks: {ex.Message}");
            return 0;
        }
    }
    
    // Get user's saved/liked tracks
    public async Task<List<PlaylistTrack>?> GetLikedTracksAsync(int limit = 50, int offset = 0)
    {
        var token = await GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }
        
        try 
        {
            // Spotify API has a maximum limit of 50 for this endpoint
            int validLimit = Math.Min(limit, 50);
            
            var request = new HttpRequestMessage(
                HttpMethod.Get, 
                $"https://api.spotify.com/v1/me/tracks?limit={validLimit}&offset={offset}"
            );
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                // Log the raw content for debugging
                await _jsRuntime.InvokeVoidAsync("console.log", "Liked tracks API response:", content.Substring(0, Math.Min(500, content.Length)));
                
                try 
                {
                    var options = new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true
                    };
                    
                    // The Spotify API for saved tracks returns data in a slightly different format
                    // Parse it manually to match our PlaylistTrack structure
                    using (JsonDocument doc = JsonDocument.Parse(content))
                    {
                        var tracks = new List<PlaylistTrack>();
                        var root = doc.RootElement;
                        
                        if (root.TryGetProperty("items", out var items))
                        {
                            foreach (var item in items.EnumerateArray())
                            {
                                var track = new PlaylistTrack();
                                
                                // Get the added_at property
                                if (item.TryGetProperty("added_at", out var addedAt))
                                {
                                    if (DateTime.TryParse(addedAt.GetString(), out var parsedDate))
                                    {
                                        track.AddedAt = parsedDate;
                                    }
                                }
                                
                                // Get the track object
                                if (item.TryGetProperty("track", out var trackElement))
                                {
                                    var spotifyTrack = JsonSerializer.Deserialize<SpotifyTrack>(
                                        trackElement.GetRawText(), options);
                                    
                                    track.Track = spotifyTrack;
                                }
                                
                                tracks.Add(track);
                            }
                        }
                        
                        return tracks;
                    }
                }
                catch (Exception ex)
                {
                    await _jsRuntime.InvokeVoidAsync("console.error", $"Error parsing liked tracks: {ex.Message}");
                    return null;
                }
            }
            
            // Get error details from response
            var errorContent = await response.Content.ReadAsStringAsync();
            await _jsRuntime.InvokeVoidAsync("console.error", $"Failed to get liked tracks: {response.StatusCode}");
            await _jsRuntime.InvokeVoidAsync("console.error", $"Error details: {errorContent}");
            return null;
        }
        catch (Exception ex)
        {
            await _jsRuntime.InvokeVoidAsync("console.error", $"Error getting liked tracks: {ex.Message}");
            return null;
        }
    }
    
    // Get user's playlists count
    public async Task<int> GetPlaylistsCountAsync()
    {
        var token = await GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            return 0;
        }
        
        try 
        {
            // We only need the total count, so limit=1 is sufficient
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me/playlists?limit=1");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var paginatedResponse = JsonSerializer.Deserialize<PaginatedResponse>(content, options);
                
                return paginatedResponse?.Total ?? 0;
            }
            
            await _jsRuntime.InvokeVoidAsync("console.error", $"Failed to get playlists: {response.StatusCode}");
            return 0;
        }
        catch (Exception ex)
        {
            await _jsRuntime.InvokeVoidAsync("console.error", $"Error getting playlists: {ex.Message}");
            return 0;
        }
    }
    
    // Get user's playlists with details
    public async Task<List<SpotifyPlaylist>?> GetPlaylistsAsync(int limit = 20, int offset = 0)
    {
        var token = await GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }
        
        try 
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get, 
                $"https://api.spotify.com/v1/me/playlists?limit={limit}&offset={offset}"
            );
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var paginatedResponse = JsonSerializer.Deserialize<PaginatedPlaylistsResponse>(content, options);
                
                return paginatedResponse?.Items;
            }
            
            await _jsRuntime.InvokeVoidAsync("console.error", $"Failed to get playlists: {response.StatusCode}");
            return null;
        }
        catch (Exception ex)
        {
            await _jsRuntime.InvokeVoidAsync("console.error", $"Error getting playlists: {ex.Message}");
            return null;
        }
    }
    
    // Get user's playlists with a virtual Liked Songs playlist at the top
    public async Task<List<SpotifyPlaylist>?> GetPlaylistsWithLikedSongs(int limit = 20, int offset = 0)
    {
        var token = await GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }
        
        try 
        {
            // Create parallel tasks to fetch playlists and liked songs count
            var playlistsTask = GetPlaylistsAsync(limit, offset);
            var likedSongsCountTask = GetSavedTracksCountAsync();
            
            await Task.WhenAll(playlistsTask, likedSongsCountTask);
            
            var playlists = await playlistsTask;
            var likedSongsCount = await likedSongsCountTask;
            
            if (playlists == null)
            {
                playlists = new List<SpotifyPlaylist>();
            }
            
            // Add a virtual Liked Songs playlist at the top
            playlists.Insert(0, new SpotifyPlaylist 
            {
                Id = "liked",
                Name = "Liked Songs",
                Description = "Songs you've liked on Spotify",
                Images = new List<ImageInfo> 
                { 
                    new ImageInfo 
                    { 
                        Url = "https://misc.scdn.co/liked-songs/liked-songs-300.png",
                        Height = 300,
                        Width = 300
                    } 
                },
                Tracks = new PlaylistTracksRef { Total = likedSongsCount },
                ExternalUrls = new ExternalUrls { Spotify = "https://open.spotify.com/collection/tracks" }
            });
            
            return playlists;
        }
        catch (Exception ex)
        {
            await _jsRuntime.InvokeVoidAsync("console.error", $"Error getting playlists with liked songs: {ex.Message}");
            return null;
        }
    }
    
    // Get tracks for a specific playlist, handling both regular playlists and the liked songs playlist
    public async Task<List<PlaylistTrack>?> GetPlaylistTracksAsync(string playlistId, int limit = 50, int offset = 0)
    {
        if (string.IsNullOrEmpty(playlistId))
        {
            Console.Error.WriteLine("GetPlaylistTracksAsync: playlistId is null or empty");
            return null;
        }
        
        // Special case for liked songs playlist
        if (playlistId == "liked")
        {
            return await GetLikedSongsPlaylistTracksAsync(limit, offset);
        }
        
        // Regular playlist tracks
        return await GetRegularPlaylistTracksAsync(playlistId, limit, offset);
    }
    
    // Private method to get liked songs as a playlist
    private async Task<List<PlaylistTrack>?> GetLikedSongsPlaylistTracksAsync(int limit = 50, int offset = 0)
    {
        Console.WriteLine($"GetLikedSongsPlaylistTracksAsync: Fetching liked songs, limit={limit}, offset={offset}");
        return await GetLikedTracksAsync(limit, offset);
    }
    
    // Private method to get tracks from a regular playlist
    private async Task<List<PlaylistTrack>?> GetRegularPlaylistTracksAsync(string playlistId, int limit = 50, int offset = 0)
    {
        var token = await GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            Console.Error.WriteLine("GetRegularPlaylistTracksAsync: No token available");
            return null;
        }
        
        try 
        {
            // Make sure limit isn't larger than what the API supports (usually 50 or 100)
            int validLimit = Math.Min(limit, 50);
            
            Console.WriteLine($"GetRegularPlaylistTracksAsync: Fetching tracks for playlist {playlistId}, limit={validLimit}, offset={offset}");
            
            var request = new HttpRequestMessage(
                HttpMethod.Get, 
                $"https://api.spotify.com/v1/playlists/{playlistId}/tracks?limit={validLimit}&offset={offset}"
            );
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var paginatedResponse = JsonSerializer.Deserialize<PaginatedPlaylistTracksResponse>(content, options);
                
                Console.WriteLine($"GetRegularPlaylistTracksAsync: Retrieved {paginatedResponse?.Items?.Count ?? 0} tracks");
                return paginatedResponse?.Items;
            }
            
            Console.Error.WriteLine($"GetRegularPlaylistTracksAsync: Failed to get playlist tracks: {response.StatusCode}");
            return null;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"GetRegularPlaylistTracksAsync: Error getting playlist tracks: {ex.Message}");
            return null;
        }
    }
    
    // Get count of artists the user is following
    public async Task<int> GetFollowedArtistsCountAsync()
    {
        var token = await GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            return 0;
        }
        
        try 
        {
            // Log that we're making the request
            await _jsRuntime.InvokeVoidAsync("console.log", "Requesting followed artists from Spotify API...");
            
            // The following API is structured differently, it returns a "artists" object with total
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me/following?type=artist&limit=1");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            var response = await _httpClient.SendAsync(request);
            
            // Log the response status
            await _jsRuntime.InvokeVoidAsync("console.log", $"Followed artists response status: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                // Parse the JSON manually since the structure is different
                using (JsonDocument doc = JsonDocument.Parse(content))
                {
                    if (doc.RootElement.TryGetProperty("artists", out JsonElement artists))
                    {
                        if (artists.TryGetProperty("total", out JsonElement total))
                        {
                            if (total.TryGetInt32(out int totalCount))
                            {
                                await _jsRuntime.InvokeVoidAsync("console.log", $"Retrieved followed artists count: {totalCount}");
                                return totalCount;
                            }
                        }
                    }
                    
                    // Log JSON structure for debugging
                    await _jsRuntime.InvokeVoidAsync("console.log", $"JSON response structure: {content}");
                }
                
                return 0;
            }
            
            // More detailed error logging
            var errorContent = await response.Content.ReadAsStringAsync();
            await _jsRuntime.InvokeVoidAsync("console.error", $"Failed to get followed artists: {response.StatusCode}");
            await _jsRuntime.InvokeVoidAsync("console.error", $"Error details: {errorContent}");
            
            return 0;
        }
        catch (Exception ex)
        {
            await _jsRuntime.InvokeVoidAsync("console.error", $"Error getting followed artists: {ex.Message}");
            return 0;
        }
    }

    // Fetch current playback state
    public async Task<PlaybackStateData?> GetCurrentPlaybackStateAsync()
    {
        try
        {
            Console.WriteLine("GetCurrentPlaybackStateAsync: Starting to fetch playback state");
            var token = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("GetCurrentPlaybackStateAsync: No token available");
                return null;
            }
            
            Console.WriteLine("GetCurrentPlaybackStateAsync: Token available, calling Spotify API");
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me/player");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            var response = await _httpClient.SendAsync(request);
            
            Console.WriteLine($"GetCurrentPlaybackStateAsync: Player API response status: {response.StatusCode}");
            
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                Console.WriteLine("GetCurrentPlaybackStateAsync: No active playback session found");
                return null;
            }
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"GetCurrentPlaybackStateAsync: Received response of length {content.Length}");
                
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                
                // Debug log the JSON keys at the root level
                Console.WriteLine("GetCurrentPlaybackStateAsync: JSON root keys: " + string.Join(", ", root.EnumerateObject().Select(p => p.Name)));
                
                var playbackState = new PlaybackStateData();
                
                if (root.TryGetProperty("is_playing", out var isPlayingProp))
                {
                    playbackState.IsPlaying = isPlayingProp.GetBoolean();
                    Console.WriteLine($"GetCurrentPlaybackStateAsync: is_playing = {playbackState.IsPlaying}");
                    
                    if (root.TryGetProperty("item", out var item))
                    {
                        Console.WriteLine("GetCurrentPlaybackStateAsync: Found 'item' property");
                        
                        if (item.TryGetProperty("name", out var nameProp))
                        {
                            playbackState.TrackName = nameProp.GetString() ?? "Not Playing";
                        }
                        
                        if (item.TryGetProperty("artists", out var artistsArray))
                        {
                            var artistNames = new List<string>();
                            foreach (var artist in artistsArray.EnumerateArray())
                            {
                                if (artist.TryGetProperty("name", out var artistNameProp))
                                {
                                    artistNames.Add(artistNameProp.GetString() ?? "");
                                }
                            }
                            playbackState.ArtistName = string.Join(", ", artistNames);
                        }
                        
                        if (item.TryGetProperty("album", out var album) && 
                            album.TryGetProperty("images", out var imagesArray))
                        {
                            var images = imagesArray.EnumerateArray();
                            if (images.Any())
                            {
                                var firstImage = images.First();
                                if (firstImage.TryGetProperty("url", out var urlProp))
                                {
                                    playbackState.ImageUrl = urlProp.GetString() ?? "";
                                }
                            }
                        }
                        
                        Console.WriteLine($"GetCurrentPlaybackStateAsync: Parsed playback state: {(playbackState.IsPlaying ? "Playing" : "Paused")} - {playbackState.TrackName} by {playbackState.ArtistName}");
                        return playbackState;
                    }
                    else
                    {
                        Console.WriteLine("GetCurrentPlaybackStateAsync: 'item' property not found in response");
                    }
                }
                else
                {
                    Console.WriteLine("GetCurrentPlaybackStateAsync: 'is_playing' property not found in response");
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.Error.WriteLine($"GetCurrentPlaybackStateAsync: Error fetching current playback: {response.StatusCode}");
                Console.Error.WriteLine($"GetCurrentPlaybackStateAsync: Error details: {errorContent}");
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"GetCurrentPlaybackStateAsync: Exception: {ex.Message}");
            return null;
        }
    }
    
    // Fetch recently played tracks
    public async Task<PlaybackStateData?> GetRecentlyPlayedTrackAsync()
    {
        try
        {
            Console.WriteLine("GetRecentlyPlayedTrackAsync: Starting to fetch recently played tracks");
            var token = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("GetRecentlyPlayedTrackAsync: No token available");
                return null;
            }
            
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me/player/recently-played?limit=1");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            var response = await _httpClient.SendAsync(request);
            
            Console.WriteLine($"GetRecentlyPlayedTrackAsync: API response status: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
                {
                    var firstItem = items[0];
                    var result = new PlaybackStateData
                    {
                        IsPlaying = false // Recently played track is not currently playing
                    };
                    
                    if (firstItem.TryGetProperty("track", out var track))
                    {
                        if (track.TryGetProperty("name", out var nameProp))
                        {
                            result.TrackName = $"Last played: {nameProp.GetString() ?? "Not Playing"}";
                        }
                        
                        if (track.TryGetProperty("artists", out var artistsArray))
                        {
                            var artistNames = new List<string>();
                            foreach (var artist in artistsArray.EnumerateArray())
                            {
                                if (artist.TryGetProperty("name", out var artistNameProp))
                                {
                                    artistNames.Add(artistNameProp.GetString() ?? "");
                                }
                            }
                            result.ArtistName = string.Join(", ", artistNames);
                        }
                        
                        if (track.TryGetProperty("album", out var album) && 
                            album.TryGetProperty("images", out var imagesArray))
                        {
                            var images = imagesArray.EnumerateArray();
                            if (images.Any())
                            {
                                var firstImage = images.First();
                                if (firstImage.TryGetProperty("url", out var urlProp))
                                {
                                    result.ImageUrl = urlProp.GetString() ?? "";
                                }
                            }
                        }
                        
                        Console.WriteLine($"GetRecentlyPlayedTrackAsync: Found recently played track: {result.TrackName} by {result.ArtistName}");
                        return result;
                    }
                }
                else
                {
                    Console.WriteLine("GetRecentlyPlayedTrackAsync: No recently played tracks found");
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.Error.WriteLine($"GetRecentlyPlayedTrackAsync: Error fetching recently played tracks: {response.StatusCode}");
                Console.Error.WriteLine($"GetRecentlyPlayedTrackAsync: Error details: {errorContent}");
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"GetRecentlyPlayedTrackAsync: Exception: {ex.Message}");
            return null;
        }
    }
    
    // Get a playlist by its ID, supporting both regular playlists and the special "liked" songs playlist
    public async Task<SpotifyPlaylist?> GetPlaylistByIdAsync(string playlistId)
    {
        Console.WriteLine($"GetPlaylistByIdAsync: Starting to fetch playlist with ID: {playlistId}");
        
        // Handle special case for liked songs
        if (playlistId == "liked")
        {
            Console.WriteLine("GetPlaylistByIdAsync: Handling special case for liked songs");
            var likedSongsCount = await GetSavedTracksCountAsync();
            
            return new SpotifyPlaylist 
            {
                Id = "liked",
                Name = "Liked Songs",
                Description = "Songs you've liked on Spotify",
                Images = new List<ImageInfo> 
                { 
                    new ImageInfo 
                    { 
                        Url = "https://misc.scdn.co/liked-songs/liked-songs-300.png",
                        Height = 300,
                        Width = 300
                    } 
                },
                Tracks = new PlaylistTracksRef { Total = likedSongsCount },
                ExternalUrls = new ExternalUrls { Spotify = "https://open.spotify.com/collection/tracks" }
            };
        }
        
        // For regular playlist
        var token = await GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("GetPlaylistByIdAsync: No token available");
            return null;
        }
        
        try 
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get, 
                $"https://api.spotify.com/v1/playlists/{playlistId}"
            );
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            var response = await _httpClient.SendAsync(request);
            Console.WriteLine($"GetPlaylistByIdAsync: API response status: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var playlist = JsonSerializer.Deserialize<SpotifyPlaylist>(content, options);
                
                Console.WriteLine($"GetPlaylistByIdAsync: Retrieved playlist '{playlist?.Name}' with {playlist?.Tracks?.Total} tracks");
                return playlist;
            }
            
            // Handle error cases
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.Error.WriteLine($"GetPlaylistByIdAsync: Failed to get playlist: {response.StatusCode}");
            Console.Error.WriteLine($"GetPlaylistByIdAsync: Error details: {errorContent}");
            return null;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"GetPlaylistByIdAsync: Error getting playlist: {ex.Message}");
            return null;
        }
    }
}