using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SpotMe.Web.Domain.Users;
using SpotMe.Web.Persistency;
using SpotMe.Web.Services;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;

namespace SpotMe.Web.Endpoints.Spotify.PlaylistTracks;

public class GetPlaylistTracksEndpoint : Endpoint<GetPlaylistTracksRequest, GetPlaylistTracksResponse>
{
    private readonly DatabaseContext _context;
    private readonly IHttpClientFactory _httpClientFactory;

    public GetPlaylistTracksEndpoint(DatabaseContext context, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
    }

    public override void Configure()
    {
        Get("/spotify/playlists/{PlaylistId}/tracks");
    }

    public override async Task HandleAsync(GetPlaylistTracksRequest req, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var spotifyToken = await _context.SpotifyTokens
            .FirstOrDefaultAsync(st => st.UserId == userId && st.ExpiresAt > DateTime.UtcNow, ct);

        if (spotifyToken == null)
        {
            await SendAsync(new GetPlaylistTracksResponse(), 401, ct);
            return;
        }

        var httpClient = _httpClientFactory.CreateClient();
        
        // Handle special case for "liked" songs
        string apiUrl;
        if (req.PlaylistId == "liked")
        {
            apiUrl = $"https://api.spotify.com/v1/me/tracks?limit={req.Limit}&offset={req.Offset}";
        }
        else
        {
            apiUrl = $"https://api.spotify.com/v1/playlists/{req.PlaylistId}/tracks?limit={req.Limit}&offset={req.Offset}";
        }

        var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, apiUrl);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", spotifyToken.AccessToken);

        var response = await httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            await SendAsync(new GetPlaylistTracksResponse(), (int)response.StatusCode, ct);
            return;
        }

        var content = await response.Content.ReadAsStringAsync(ct);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        List<PlaylistTrack>? tracks;
        int total = 0;

        if (req.PlaylistId == "liked")
        {
            // Parse liked songs format
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            total = root.TryGetProperty("total", out var totalProp) ? totalProp.GetInt32() : 0;
            
            tracks = new List<PlaylistTrack>();
            if (root.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    var track = new PlaylistTrack();
                    if (item.TryGetProperty("added_at", out var addedAt))
                    {
                        if (DateTime.TryParse(addedAt.GetString(), out var parsedDate))
                        {
                            track.AddedAt = parsedDate;
                        }
                    }
                    if (item.TryGetProperty("track", out var trackElement))
                    {
                        track.Track = JsonSerializer.Deserialize<SpotifyTrack>(trackElement.GetRawText(), options);
                    }
                    tracks.Add(track);
                }
            }
        }
        else
        {
            var paginatedResponse = JsonSerializer.Deserialize<PaginatedPlaylistTracksResponse>(content, options);
            tracks = paginatedResponse?.Items;
            total = paginatedResponse?.Total ?? 0;
        }

        await SendOkAsync(new GetPlaylistTracksResponse
        {
            Tracks = tracks,
            Total = total
        }, ct);
    }
}

public class GetPlaylistTracksRequestValidator : Validator<GetPlaylistTracksRequest>
{
    public GetPlaylistTracksRequestValidator()
    {
        RuleFor(x => x.PlaylistId)
            .NotEmpty();
    }
}

