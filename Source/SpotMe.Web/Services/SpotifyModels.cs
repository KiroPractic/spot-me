using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SpotMe.Web.Services
{
    // Model for Spotify user profile
    public class SpotifyUserProfile
    {
        public string? Id { get; set; }
        
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
    }
    
    public class FollowersInfo
    {
        public int Total { get; set; }
    }
    
    public class ImageInfo
    {
        public string? Url { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
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
}