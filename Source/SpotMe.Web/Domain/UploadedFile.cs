namespace SpotMe.Web.Domain;

public sealed class UploadedFile : Entity
{
    public Guid UserId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty; // SHA256 hash of file content
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

