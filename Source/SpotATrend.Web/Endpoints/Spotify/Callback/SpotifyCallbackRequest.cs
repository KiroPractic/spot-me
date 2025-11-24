namespace SpotATrend.Web.Endpoints.Spotify.Callback;

public class SpotifyCallbackRequest
{
    public string Code { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

