using SpotATrend.Web.Services;

namespace SpotATrend.Web.Endpoints.Spotify.Playlists;

public class GetPlaylistsResponse
{
    public List<SpotifyPlaylist>? Playlists { get; set; }
    public int Total { get; set; }
}

