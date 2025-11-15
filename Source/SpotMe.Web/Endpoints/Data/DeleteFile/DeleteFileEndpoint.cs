using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SpotMe.Web.Persistency;
using System.Security.Claims;

namespace SpotMe.Web.Endpoints.Data.DeleteFile;

public class DeleteFileEndpoint : Endpoint<DeleteFileRequest, DeleteFileResponse>
{
    private readonly DatabaseContext _context;
    private readonly ILogger<DeleteFileEndpoint> _logger;

    public DeleteFileEndpoint(DatabaseContext context, ILogger<DeleteFileEndpoint> logger)
    {
        _context = context;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/data/delete");
    }

    public override async Task HandleAsync(DeleteFileRequest req, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        // Delete all streaming history for the user (since we're using database now)
        var entries = await _context.StreamingHistory
            .Where(sh => sh.UserId == userId)
            .ToListAsync(ct);

        if (!entries.Any())
        {
            await SendOkAsync(new DeleteFileResponse
            {
                Message = "No data found to delete"
            }, ct);
            return;
        }

        _context.StreamingHistory.RemoveRange(entries);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Deleted {Count} streaming history entries for user {UserId}", entries.Count, userId);

        await SendOkAsync(new DeleteFileResponse
        {
            Message = $"Deleted {entries.Count} entries"
        }, ct);
    }
}

public class DeleteFileRequestValidator : Validator<DeleteFileRequest>
{
    public DeleteFileRequestValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty();
    }
}

