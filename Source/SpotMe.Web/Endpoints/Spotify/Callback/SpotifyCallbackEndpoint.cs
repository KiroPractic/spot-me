using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using SpotMe.Web.Domain.Users;
using SpotMe.Web.Persistency;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace SpotMe.Web.Endpoints.Spotify.Callback;

public class SpotifyCallbackEndpoint : Endpoint<SpotifyCallbackRequest, SpotifyCallbackResponse>
{
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly DatabaseContext _context;
    private readonly HttpClient _httpClient;

    public SpotifyCallbackEndpoint(
        IConfiguration configuration,
        IMemoryCache cache,
        DatabaseContext context,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _cache = cache;
        _context = context;
        _httpClient = httpClientFactory.CreateClient();
    }

    public override void Configure()
    {
        Get("/spotify/callback");
        AllowAnonymous();
    }

    public override async Task HandleAsync(SpotifyCallbackRequest req, CancellationToken ct)
    {
        // Verify state and get user ID
        if (!_cache.TryGetValue($"spotify_auth_state_{req.State}", out var cachedUserId) || cachedUserId == null)
        {
            HttpContext.Response.Redirect("/login.html?error=invalid_state");
            await Task.CompletedTask;
            return;
        }

        var userIdString = cachedUserId.ToString();
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            HttpContext.Response.Redirect("/login.html?error=invalid_user");
            await Task.CompletedTask;
            return;
        }

        // Remove state from cache
        _cache.Remove($"spotify_auth_state_{req.State}");

        var clientId = _configuration["Spotify:ClientId"];
        var clientSecret = _configuration["Spotify:ClientSecret"];
        var redirectUri = _configuration["Spotify:RedirectUri"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            HttpContext.Response.Redirect("/login.html?error=config_missing");
            await Task.CompletedTask;
            return;
        }

        // Exchange code for token
        var tokenRequest = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, "https://accounts.spotify.com/api/token");
        var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        tokenRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);
        
        var formData = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "authorization_code"),
            new("code", req.Code),
            new("redirect_uri", redirectUri!)
        };
        tokenRequest.Content = new FormUrlEncodedContent(formData);

        var tokenResponse = await _httpClient.SendAsync(tokenRequest, ct);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            HttpContext.Response.Redirect("/login.html?error=token_exchange_failed");
            await Task.CompletedTask;
            return;
        }

        var tokenContent = await tokenResponse.Content.ReadAsStringAsync(ct);
        var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenContent);

        var accessToken = tokenData.GetProperty("access_token").GetString();
        var refreshToken = tokenData.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
        var expiresIn = tokenData.GetProperty("expires_in").GetInt32();
        var expiresAt = DateTime.UtcNow.AddSeconds(expiresIn);

        // User ID is already extracted from state cache above

        // Store or update Spotify token
        var existingToken = await _context.SpotifyTokens
            .FirstOrDefaultAsync(st => st.UserId == userId, ct);

        if (existingToken != null)
        {
            existingToken.AccessToken = accessToken!;
            existingToken.RefreshToken = refreshToken;
            existingToken.ExpiresAt = expiresAt;
            existingToken.TokenType = "Bearer";
        }
        else
        {
            var spotifyToken = new SpotifyToken
            {
                UserId = userId,
                AccessToken = accessToken!,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                TokenType = "Bearer"
            };
            _context.SpotifyTokens.Add(spotifyToken);
        }

        await _context.SaveChangesAsync(ct);

        // Redirect to playlists page
        HttpContext.Response.Redirect("/playlists.html?spotify_connected=true");
        await Task.CompletedTask;
    }
}

public class SpotifyCallbackRequestValidator : Validator<SpotifyCallbackRequest>
{
    public SpotifyCallbackRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty();

        RuleFor(x => x.State)
            .NotEmpty();
    }
}

