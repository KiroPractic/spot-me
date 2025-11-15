namespace SpotMe.Web.Endpoints.Spotify.Callback;

public class SpotifyCallbackResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? RedirectUrl { get; set; }
}

