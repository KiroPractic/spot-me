using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using SpotMe.Web.Persistency;
using SpotMe.Web.Services;
using System.Security.Claims;

namespace SpotMe.Web.Endpoints.Data.GetFiles;

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

        var entryCount = await _context.StreamingHistory
            .Where(sh => sh.UserId == userId)
            .CountAsync(ct);

        var files = new List<UserDataFileInfo>();

        if (entryCount > 0)
        {
            var dateRange = await _context.StreamingHistory
                .Where(sh => sh.UserId == userId)
                .Select(sh => new { sh.PlayedAt })
                .OrderBy(sh => sh.PlayedAt)
                .ToListAsync(ct);

            var earliestDate = dateRange.FirstOrDefault()?.PlayedAt ?? DateTime.Now;
            var latestDate = dateRange.LastOrDefault()?.PlayedAt ?? DateTime.Now;

            files.Add(new UserDataFileInfo
            {
                FileName = "Streaming History Database",
                FileSize = 0,
                UploadedAt = earliestDate,
                EntryCount = entryCount,
                IsDatabaseEntry = true,
                DateRange = $"{earliestDate:yyyy-MM-dd} to {latestDate:yyyy-MM-dd}"
            });
        }

        await SendOkAsync(new GetFilesResponse { Files = files }, ct);
    }
}

