using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using SpotATrend.Web.Domain.Users;
using SpotATrend.Web.Persistency;
using SpotATrend.Web.Services;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;

namespace SpotATrend.Web.Endpoints.Spotify.Playlists;

public class GetPlaylistsEndpoint : Endpoint<GetPlaylistsRequest, GetPlaylistsResponse>
{
    private readonly DatabaseContext _context;
    private readonly IHttpClientFactory _httpClientFactory;

    public GetPlaylistsEndpoint(DatabaseContext context, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
    }

    public override void Configure()
    {
        Get("/spotify/playlists");
    }

    public override async Task HandleAsync(GetPlaylistsRequest req, CancellationToken ct)
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
            await SendAsync(new GetPlaylistsResponse(), 401, ct);
            return;
        }

        var httpClient = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(
            System.Net.Http.HttpMethod.Get,
            $"https://api.spotify.com/v1/me/playlists?limit={req.Limit}&offset={req.Offset}");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", spotifyToken.AccessToken);

        var response = await httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            await SendAsync(new GetPlaylistsResponse(), (int)response.StatusCode, ct);
            return;
        }

        var content = await response.Content.ReadAsStringAsync(ct);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var paginatedResponse = JsonSerializer.Deserialize<PaginatedPlaylistsResponse>(content, options);

        await SendOkAsync(new GetPlaylistsResponse
        {
            Playlists = paginatedResponse?.Items,
            Total = paginatedResponse?.Total ?? 0
        }, ct);
    }
}

