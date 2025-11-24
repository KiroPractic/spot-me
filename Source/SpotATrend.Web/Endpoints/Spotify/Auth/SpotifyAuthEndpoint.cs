using FastEndpoints;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using SpotATrend.Web.Services;
using System.Security.Claims;

namespace SpotATrend.Web.Endpoints.Spotify.Auth;

public class SpotifyAuthEndpoint : Endpoint<SpotifyAuthRequest, SpotifyAuthResponse>
{
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;

    public SpotifyAuthEndpoint(IConfiguration configuration, IMemoryCache cache)
    {
        _configuration = configuration;
        _cache = cache;
    }

    public override void Configure()
    {
        Get("/spotify/auth");
    }

    public override async Task HandleAsync(SpotifyAuthRequest req, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var clientId = _configuration["Spotify:ClientId"];
        var redirectUri = _configuration["Spotify:RedirectUri"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(redirectUri))
        {
            await SendAsync(new SpotifyAuthResponse(), 500, ct);
            return;
        }

        // Generate state for CSRF protection
        var state = Guid.NewGuid().ToString();
        
        // Store state and user ID in cache for 10 minutes
        _cache.Set($"spotify_auth_state_{state}", userIdClaim.Value, TimeSpan.FromMinutes(10));

        // Required scopes for Spotify Web Playback SDK and library access
        const string requiredScopes = "streaming user-read-email user-read-private user-read-playback-state user-modify-playback-state user-library-read user-follow-read playlist-read-private playlist-read-collaborative user-read-recently-played";

        var queryParams = new Dictionary<string, string?>
        {
            { "client_id", clientId },
            { "response_type", "code" },
            { "redirect_uri", redirectUri },
            { "scope", requiredScopes },
            { "state", state },
            { "show_dialog", "true" }
        };

        var authUrl = QueryHelpers.AddQueryString("https://accounts.spotify.com/authorize", queryParams);

        await SendOkAsync(new SpotifyAuthResponse
        {
            AuthUrl = authUrl,
            State = state
        }, ct);
    }
}

