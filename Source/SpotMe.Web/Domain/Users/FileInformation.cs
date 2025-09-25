namespace SpotMe.Web.Domain.Users;

public sealed class FileInformation : Entity
{
    public FileInformation(string originalFileName, string extension, string contentType, string link)
    {
        ObjectId = Guid.NewGuid();
        UpdatedOn = DateTimeOffset.Now;
        OriginalFileName = originalFileName;
        Extension = extension;
        ContentType = contentType;
        Link = link;
    }

    public string? Title { get; set; }
    public string? Description { get; set; }

    public DateTimeOffset UpdatedOn { get; private set; }
    public Guid ObjectId { get; private set; }
    public string OriginalFileName { get; private set; }
    public string Extension { get; private set; }
    public string ContentType { get; private set; }
    public string Link { get; private set; }

    public void Update(string originalFileName, string extension, string contentType, string link)
    {
        UpdatedOn = DateTimeOffset.Now;
        OriginalFileName = originalFileName;
        Extension = extension;
        ContentType = contentType;
        Link = link;
    }

#pragma warning disable CS8618, CS9264
    private FileInformation() { }
#pragma warning restore CS8618, CS9264
} 