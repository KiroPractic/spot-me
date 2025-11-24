using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using SpotATrend.Web.Domain;
using SpotATrend.Web.Persistency;
using SpotATrend.Web.Services;
using System.Security.Claims;

namespace SpotATrend.Web.Endpoints.Data.GetFiles;

public class GetFilesEndpoint : EndpointWithoutRequest<GetFilesResponse>
{
    private readonly DatabaseContext _context;

    public GetFilesEndpoint(DatabaseContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("/data/files");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        // Get all uploaded files for this user
        var uploadedFiles = await _context.UploadedFiles
            .Where(uf => uf.UserId == userId)
            .OrderByDescending(uf => uf.UploadedAt)
            .ToListAsync(ct);

        if (!uploadedFiles.Any())
        {
            await SendOkAsync(new GetFilesResponse { Files = new List<UserDataFileInfo>() }, ct);
            return;
        }

        // Get date ranges and entry counts for all files in a single query
        var fileIds = uploadedFiles.Select(uf => uf.Id).ToList();
        var fileStats = await _context.StreamingHistory
            .Where(sh => sh.UserId == userId && fileIds.Contains(sh.UploadedFileId))
            .GroupBy(sh => sh.UploadedFileId)
            .Select(g => new
            {
                FileId = g.Key,
                EntryCount = g.Count(),
                EarliestDate = g.Min(sh => sh.PlayedAt),
                LatestDate = g.Max(sh => sh.PlayedAt)
            })
            .ToDictionaryAsync(x => x.FileId, ct);

        var files = uploadedFiles.Select(uploadedFile =>
        {
            string? dateRangeStr = null;
            int entryCount = 0;
            
            // Get count and date range from the query results
            if (fileStats.TryGetValue(uploadedFile.Id, out var stats))
            {
                entryCount = stats.EntryCount;
                dateRangeStr = $"{stats.EarliestDate:yyyy-MM-dd} to {stats.LatestDate:yyyy-MM-dd}";
            }

            return new UserDataFileInfo
            {
                FileId = uploadedFile.Id.ToString(),
                FileName = uploadedFile.FileName,
                FileSize = uploadedFile.FileSize,
                UploadedAt = uploadedFile.UploadedAt,
                EntryCount = entryCount,
                IsDatabaseEntry = false,
                DateRange = dateRangeStr
            };
        }).ToList();

        await SendOkAsync(new GetFilesResponse { Files = files }, ct);
    }
}

