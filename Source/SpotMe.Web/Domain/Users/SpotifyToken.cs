namespace SpotMe.Web.Domain.Users;

public sealed class SpotifyToken : Entity
{
    public Guid UserId { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? TokenType { get; set; } = "Bearer";
    public User? User { get; set; }
}

