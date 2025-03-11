using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SpotMe.Web.Services
{
    // Model for Spotify user profile
    public class SpotifyUserProfile
    {
        public string? Id { get; set; }
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
}