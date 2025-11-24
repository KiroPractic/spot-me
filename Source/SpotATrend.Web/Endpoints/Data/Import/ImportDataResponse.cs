namespace SpotATrend.Web.Endpoints.Data.Import;

public class ImportDataResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? ImportedCount { get; set; }
    public bool AlreadyImported { get; set; }
}

