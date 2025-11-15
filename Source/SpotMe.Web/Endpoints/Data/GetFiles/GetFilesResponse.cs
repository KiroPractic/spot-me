namespace SpotMe.Web.Endpoints.Data.GetFiles;

public class GetFilesResponse
{
    public List<UserDataFileInfo> Files { get; set; } = new();
}

public class UserDataFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    public int EntryCount { get; set; }
    public bool IsDatabaseEntry { get; set; }
    public string? DateRange { get; set; }
}

