using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SpotMe.Web.Domain.Users;
using SpotMe.Web.Persistency;
using SpotMe.Web.Services;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;

namespace SpotMe.Web.Endpoints.Spotify.Profile;

public class GetProfileEndpoint : EndpointWithoutRequest<GetProfileResponse>
{
    private readonly DatabaseContext _context;
    private readonly IHttpClientFactory _httpClientFactory;

    public GetProfileEndpoint(DatabaseContext context, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
    }

    public override void Configure()
    {
        Get("/spotify/profile");
    }

    public override async Task HandleAsync(CancellationToken ct)
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
            await SendAsync(new GetProfileResponse(), 401, ct);
            return;
        }

        var httpClient = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, "https://api.spotify.com/v1/me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", spotifyToken.AccessToken);

        var response = await httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            await SendAsync(new GetProfileResponse(), (int)response.StatusCode, ct);
            return;
        }

        var content = await response.Content.ReadAsStringAsync(ct);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var profile = JsonSerializer.Deserialize<SpotifyUserProfile>(content, options);

        await SendOkAsync(new GetProfileResponse { Profile = profile }, ct);
    }
}

