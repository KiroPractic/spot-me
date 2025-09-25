namespace SpotMe.Web.Domain.Users;

public sealed class User : Entity
{
    private readonly List<FileInformation> _files = new();

    public required string EmailAddress { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    
    public IReadOnlyCollection<FileInformation> Files => _files.AsReadOnly();

    public void AddFile(string originalFileName, string extension, string contentType, string link)
    {
        var fileInfo = new FileInformation(originalFileName, extension, contentType, link);
        _files.Add(fileInfo);
    }

    public void RemoveFile(Guid objectId)
    {
        var file = _files.FirstOrDefault(f => f.ObjectId == objectId);
        if (file != null)
        {
            _files.Remove(file);
        }
    }

    public void RemoveFileByName(string originalFileName)
    {
        var file = _files.FirstOrDefault(f => f.OriginalFileName == originalFileName);
        if (file != null)
        {
            _files.Remove(file);
        }
    }

    public FileInformation? GetFileByName(string originalFileName)
    {
        return _files.FirstOrDefault(f => f.OriginalFileName == originalFileName);
    }
}