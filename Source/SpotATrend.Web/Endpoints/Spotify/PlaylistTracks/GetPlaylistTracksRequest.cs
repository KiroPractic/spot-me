namespace SpotATrend.Web.Endpoints.Spotify.PlaylistTracks;

public class GetPlaylistTracksRequest
{
    public string PlaylistId { get; set; } = string.Empty;
    public int Limit { get; set; } = 50;
    public int Offset { get; set; } = 0;
}

