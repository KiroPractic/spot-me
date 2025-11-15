namespace SpotMe.Web.Endpoints.Spotify.Auth;

public class SpotifyAuthResponse
{
    public string AuthUrl { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

