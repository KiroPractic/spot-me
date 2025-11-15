namespace SpotMe.Web.Endpoints.Data.DeleteFile;

public class DeleteFileRequest
{
    public string? FileId { get; set; }
    public string? FileName { get; set; } // For backward compatibility
}

