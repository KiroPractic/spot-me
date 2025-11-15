namespace SpotMe.Web.Endpoints.Spotify.Playlists;

public class GetPlaylistsRequest
{
    public int Limit { get; set; } = 20;
    public int Offset { get; set; } = 0;
}

