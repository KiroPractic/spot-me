using SpotATrend.Web.Services;

namespace SpotATrend.Web.Endpoints.Spotify.PlaylistTracks;

public class GetPlaylistTracksResponse
{
    public List<PlaylistTrack>? Tracks { get; set; }
    public int Total { get; set; }
}

