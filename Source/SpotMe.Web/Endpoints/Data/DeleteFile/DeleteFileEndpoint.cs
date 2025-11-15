using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SpotMe.Web.Domain;
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

        UploadedFile? uploadedFile = null;

        // Try to find the file by FileId first, then by FileName (for backward compatibility)
        if (!string.IsNullOrEmpty(req.FileId) && Guid.TryParse(req.FileId, out var fileId))
        {
            uploadedFile = await _context.UploadedFiles
                .FirstOrDefaultAsync(uf => uf.Id == fileId && uf.UserId == userId, ct);
        }
        else if (!string.IsNullOrEmpty(req.FileName))
        {
            uploadedFile = await _context.UploadedFiles
                .FirstOrDefaultAsync(uf => uf.FileName == req.FileName && uf.UserId == userId, ct);
        }

        if (uploadedFile == null)
        {
            await SendOkAsync(new DeleteFileResponse
            {
                Message = "File not found"
            }, ct);
            return;
        }

        // Get entry count before deletion for logging
        var entryCount = await _context.StreamingHistory
            .Where(sh => sh.UserId == userId && sh.UploadedFileId == uploadedFile.Id)
            .CountAsync(ct);

        // Delete the uploaded file record - cascade delete will automatically delete associated entries
        _context.UploadedFiles.Remove(uploadedFile);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Deleted file {FileId} ({FileName}) with {Count} streaming history entries for user {UserId}", 
            uploadedFile.Id, uploadedFile.FileName, entryCount, userId);

        await SendOkAsync(new DeleteFileResponse
        {
            Message = $"Deleted {uploadedFile.FileName} ({entryCount} entries)"
        }, ct);
    }
}

public class DeleteFileRequestValidator : Validator<DeleteFileRequest>
{
    public DeleteFileRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrEmpty(x.FileId) || !string.IsNullOrEmpty(x.FileName))
            .WithMessage("Either FileId or FileName must be provided");
    }
}

