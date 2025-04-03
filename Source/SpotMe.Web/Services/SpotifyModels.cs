using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SpotMe.Web.Services
{
    // Model for Spotify playlists
    public class SpotifyPlaylist
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        
        [JsonPropertyName("public")]
        public bool IsPublic { get; set; }
        
        [JsonPropertyName("collaborative")]
        public bool IsCollaborative { get; set; }
        
        [JsonPropertyName("snapshot_id")]
        public string? SnapshotId { get; set; }
        
        public SpotifyUser? Owner { get; set; }
        public List<ImageInfo>? Images { get; set; }
        
        [JsonPropertyName("followers")]
        public FollowersInfo? Followers { get; set; }
        
        [JsonPropertyName("tracks")]
        public PlaylistTracksRef? Tracks { get; set; }
        
        [JsonPropertyName("external_urls")]
        public ExternalUrls? ExternalUrls { get; set; }
        
        // Timestamps
        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }
        
        [JsonPropertyName("modified_at")]
        public DateTime? ModifiedAt { get; set; }
    }
    
    public class SpotifyUser
    {
        public string? Id { get; set; }
        
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }
        
        [JsonPropertyName("external_urls")]
        public ExternalUrls? ExternalUrls { get; set; }
    }
    
    public class PlaylistTracksRef
    {
        public string? Href { get; set; }
        public int Total { get; set; }
    }
    
    public class PaginatedPlaylistsResponse
    {
        public string? Href { get; set; }
        public int Limit { get; set; }
        public string? Next { get; set; }
        public int Offset { get; set; }
        public string? Previous { get; set; }
        public int Total { get; set; }
        
        [JsonPropertyName("items")]
        public List<SpotifyPlaylist>? Items { get; set; }
    }
    
    // Model for Spotify user profile
    public class SpotifyUserProfile
    {
        public string? Id { get; set; }
        
        // The Spotify API returns display_name in snake_case
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }
        
        public string? Email { get; set; }
        public string? Country { get; set; }
        public string? Product { get; set; }
        public FollowersInfo? Followers { get; set; }
        public List<ImageInfo>? Images { get; set; }
        
        [JsonPropertyName("external_urls")]
        public ExternalUrls? ExternalUrls { get; set; }
        
        public string? SpotifyUrl => ExternalUrls?.Spotify;
        
        public override string ToString()
        {
            return $"Id: {Id}, DisplayName: {DisplayName}, Email: {Email}, Images: {(Images != null ? Images.Count : 0)} images";
        }
    }
    
    public class FollowersInfo
    {
        public int Total { get; set; }
    }
    
    public class ImageInfo
    {
        public string? Url { get; set; }
        public int? Height { get; set; }
        public int? Width { get; set; }
    }
    
    public class ExternalUrls
    {
        public string? Spotify { get; set; }
    }
    
    // Generic paginated response from Spotify API
    public class PaginatedResponse
    {
        public string? Href { get; set; }
        public int Limit { get; set; }
        public string? Next { get; set; }
        public int Offset { get; set; }
        public string? Previous { get; set; }
        public int Total { get; set; }
        
        // Different endpoints have different item types, but we only need the total count
        [JsonPropertyName("items")]
        public System.Text.Json.JsonElement Items { get; set; }
    }
    
    // Model for playback state data
    public class PlaybackStateData
    {
        public bool IsPlaying { get; set; }
        public string TrackName { get; set; } = "Not Playing";
        public string ArtistName { get; set; } = "No Artist";
        public string ImageUrl { get; set; } = "";
        public bool IsPlayingOnCurrentDevice { get; set; } = false;
        public string? DeviceId { get; set; }
        public string? DeviceName { get; set; }
    }
    
    // Models for playlist tracks
    public class PlaylistTrack
    {
        public SpotifyTrack? Track { get; set; }
        
        [JsonPropertyName("added_at")]
        public DateTime? AddedAt { get; set; }
        
        [JsonPropertyName("added_by")]
        public SpotifyUser? AddedBy { get; set; }
        
        [JsonPropertyName("is_local")]
        public bool IsLocal { get; set; }
    }
    
    public class SpotifyTrack
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        
        [JsonPropertyName("duration_ms")]
        public int DurationMs { get; set; }
        
        [JsonPropertyName("track_number")]
        public int TrackNumber { get; set; }
        
        public bool Explicit { get; set; }
        public List<SpotifyArtist>? Artists { get; set; }
        public SpotifyAlbum? Album { get; set; }
        
        [JsonPropertyName("external_urls")]
        public ExternalUrls? ExternalUrls { get; set; }
        
        // Duration formatted as mm:ss
        public string FormattedDuration => TimeSpan.FromMilliseconds(DurationMs).ToString(@"m\:ss");
    }
    
    public class SpotifyArtist
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        
        [JsonPropertyName("external_urls")]
        public ExternalUrls? ExternalUrls { get; set; }
    }
    
    public class SpotifyAlbum
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public List<ImageInfo>? Images { get; set; }
        
        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }
        
        [JsonPropertyName("album_type")]
        public string? AlbumType { get; set; }
        
        [JsonPropertyName("external_urls")]
        public ExternalUrls? ExternalUrls { get; set; }
    }
    
    public class PaginatedPlaylistTracksResponse
    {
        public string? Href { get; set; }
        public int Limit { get; set; }
        public string? Next { get; set; }
        public int Offset { get; set; }
        public string? Previous { get; set; }
        public int Total { get; set; }
        
        [JsonPropertyName("items")]
        public List<PlaylistTrack>? Items { get; set; }
    }
}