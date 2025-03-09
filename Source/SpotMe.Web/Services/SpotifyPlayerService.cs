using Microsoft.JSInterop;
using System.Text.Json;

namespace SpotMe.Web.Services;

public class SpotifyPlayerService
{
    private readonly IJSRuntime _jsRuntime;
    private string? _deviceId;
    private string? _accessToken;

    public event Action<PlayerState>? OnPlayerStateChanged;
    public event Action<string>? OnPlayerError;
    public event Action<string>? OnPlayerReady;

    public SpotifyPlayerService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync(string accessToken)
    {
        if (string.IsNullOrEmpty(accessToken))
            throw new ArgumentException("Access token is required", nameof(accessToken));
        
        _accessToken = accessToken;
        
        try
        {
            // Load our JS file
            await _jsRuntime.InvokeVoidAsync("import", "./js/spotify-player.js");
            
            // Initialize the player
            _deviceId = await _jsRuntime.InvokeAsync<string>("spotifyPlayer.initialize", accessToken);
            OnPlayerReady?.Invoke(_deviceId);
        }
        catch (Exception ex)
        {
            OnPlayerError?.Invoke($"Failed to initialize player: {ex.Message}");
        }
    }

    public async Task PlayAsync(string spotifyUri)
    {
        if (string.IsNullOrEmpty(_deviceId))
        {
            OnPlayerError?.Invoke("Player not initialized");
            return;
        }

        try
        {
            await _jsRuntime.InvokeVoidAsync("spotifyPlayer.play", spotifyUri);
        }
        catch (Exception ex)
        {
            OnPlayerError?.Invoke($"Failed to play: {ex.Message}");
        }
    }

    public async Task TogglePlayAsync()
    {
        if (string.IsNullOrEmpty(_deviceId))
        {
            OnPlayerError?.Invoke("Player not initialized");
            return;
        }

        try
        {
            await _jsRuntime.InvokeVoidAsync("spotifyPlayer.togglePlay");
        }
        catch (Exception ex)
        {
            OnPlayerError?.Invoke($"Failed to toggle play: {ex.Message}");
        }
    }

    public async Task NextTrackAsync()
    {
        if (string.IsNullOrEmpty(_deviceId))
        {
            OnPlayerError?.Invoke("Player not initialized");
            return;
        }

        try
        {
            await _jsRuntime.InvokeVoidAsync("spotifyPlayer.next");
        }
        catch (Exception ex)
        {
            OnPlayerError?.Invoke($"Failed to skip to next track: {ex.Message}");
        }
    }

    public async Task PreviousTrackAsync()
    {
        if (string.IsNullOrEmpty(_deviceId))
        {
            OnPlayerError?.Invoke("Player not initialized");
            return;
        }

        try
        {
            await _jsRuntime.InvokeVoidAsync("spotifyPlayer.previous");
        }
        catch (Exception ex)
        {
            OnPlayerError?.Invoke($"Failed to return to previous track: {ex.Message}");
        }
    }

    public async Task SeekAsync(int positionMs)
    {
        if (string.IsNullOrEmpty(_deviceId))
        {
            OnPlayerError?.Invoke("Player not initialized");
            return;
        }

        try
        {
            await _jsRuntime.InvokeVoidAsync("spotifyPlayer.seek", positionMs);
        }
        catch (Exception ex)
        {
            OnPlayerError?.Invoke($"Failed to seek: {ex.Message}");
        }
    }

    public async Task SetVolumeAsync(double volumePercent)
    {
        if (string.IsNullOrEmpty(_deviceId))
        {
            OnPlayerError?.Invoke("Player not initialized");
            return;
        }

        try
        {
            await _jsRuntime.InvokeVoidAsync("spotifyPlayer.setVolume", volumePercent);
        }
        catch (Exception ex)
        {
            OnPlayerError?.Invoke($"Failed to set volume: {ex.Message}");
        }
    }

    public async Task<PlayerState?> GetPlayerStateAsync()
    {
        if (string.IsNullOrEmpty(_deviceId))
        {
            OnPlayerError?.Invoke("Player not initialized");
            return null;
        }

        try
        {
            var stateJson = await _jsRuntime.InvokeAsync<string>("spotifyPlayer.getState");
            if (string.IsNullOrEmpty(stateJson)) return null;
            
            return JsonSerializer.Deserialize<PlayerState>(stateJson);
        }
        catch (Exception ex)
        {
            OnPlayerError?.Invoke($"Failed to get player state: {ex.Message}");
            return null;
        }
    }

    public async Task DisconnectAsync()
    {
        if (string.IsNullOrEmpty(_deviceId)) return;

        try
        {
            await _jsRuntime.InvokeVoidAsync("spotifyPlayer.disconnect");
            _deviceId = null;
        }
        catch (Exception ex)
        {
            OnPlayerError?.Invoke($"Failed to disconnect player: {ex.Message}");
        }
    }
}

public class PlayerState
{
    public bool Paused { get; set; }
    public int Position { get; set; }
    public int Duration { get; set; }
    public TrackWindow? TrackWindow { get; set; }
}

public class TrackWindow
{
    public Track? CurrentTrack { get; set; }
    public List<Track>? PreviousTracks { get; set; }
    public List<Track>? NextTracks { get; set; }
}

public class Track
{
    public string? Id { get; set; }
    public string? Uri { get; set; }
    public string? Name { get; set; }
    public Album? Album { get; set; }
    public List<Artist>? Artists { get; set; }
}

public class Album
{
    public string? Uri { get; set; }
    public string? Name { get; set; }
    public List<Image>? Images { get; set; }
}

public class Artist
{
    public string? Uri { get; set; }
    public string? Name { get; set; }
}

public class Image
{
    public string? Url { get; set; }
}