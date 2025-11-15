using FastEndpoints;
using SpotMe.Web.Services;
using System.Security.Claims;

namespace SpotMe.Web.Endpoints.Data.Import;

public class ImportDataEndpoint : EndpointWithoutRequest<ImportDataResponse>
{
    private readonly StreamingHistoryImportService _importService;

    public ImportDataEndpoint(StreamingHistoryImportService importService)
    {
        _importService = importService;
    }

    public override void Configure()
    {
        Post("/data/import");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var result = await _importService.ImportUserStreamingHistoryAsync(userIdClaim.Value);

        await SendOkAsync(new ImportDataResponse
        {
            Success = result.Success,
            Message = result.AlreadyImported 
                ? $"Data already imported. {result.ExistingEntryCount} entries exist."
                : $"Successfully imported {result.ImportedCount} entries.",
            ImportedCount = result.ImportedCount,
            AlreadyImported = result.AlreadyImported
        }, ct);
    }
}

