namespace SpotATrend.Web.Endpoints.Data.Upload;

public class UploadDataResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public int? EntryCount { get; set; }
    public int? SkippedCount { get; set; }
    public int? TotalProcessed { get; set; }
}

