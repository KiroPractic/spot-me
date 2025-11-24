using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using SpotATrend.Web.Domain;
using SpotATrend.Web.Persistency;
using SpotATrend.Web.Services;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SpotATrend.Web.Endpoints.Data.Upload;

public class UploadDataEndpoint : EndpointWithoutRequest<UploadDataResponse>
{
    private readonly DatabaseContext _context;
    private readonly ILogger<UploadDataEndpoint> _logger;

    public UploadDataEndpoint(DatabaseContext context, ILogger<UploadDataEndpoint> logger)
    {
        _context = context;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/data/upload");
        AllowFileUploads();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value) || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var files = HttpContext.Request.Form.Files;
        if (files.Count == 0)
        {
            await SendOkAsync(new UploadDataResponse
            {
                Success = false,
                Message = "No file uploaded"
            }, ct);
            return;
        }

        var file = files[0];
        var fileName = file.FileName; // Get the actual uploaded filename
        
        if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            await SendOkAsync(new UploadDataResponse
            {
                Success = false,
                Message = "File must be a JSON file"
            }, ct);
            return;
        }

        const long maxFileSize = 100 * 1024 * 1024; // 100MB
        if (file.Length > maxFileSize)
        {
            await SendOkAsync(new UploadDataResponse
            {
                Success = false,
                Message = $"File size exceeds maximum allowed size (100MB)"
            }, ct);
            return;
        }

        try
        {
            // Read file content
            byte[] fileContent;
            await using (var stream = file.OpenReadStream())
            {
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream, ct);
                fileContent = memoryStream.ToArray();
            }

            // Calculate file hash (SHA256) to detect duplicate uploads
            var fileHash = ComputeFileHash(fileContent);
            
            // Check if this file has already been uploaded by this user
            var existingUpload = await _context.UploadedFiles
                .FirstOrDefaultAsync(uf => uf.UserId == userId && uf.FileHash == fileHash, ct);
            
            if (existingUpload != null)
            {
                _logger.LogWarning("User {UserId} attempted to upload duplicate file {FileName} (hash: {FileHash}). Previously uploaded on {UploadDate}",
                    userId, fileName, fileHash, existingUpload.UploadedAt);
                
                await SendOkAsync(new UploadDataResponse
                {
                    Success = false,
                    Message = $"This file has already been uploaded on {existingUpload.UploadedAt:yyyy-MM-dd HH:mm:ss}. Please upload a different file.",
                    FileName = fileName,
                    TotalProcessed = 0,
                    EntryCount = 0,
                    SkippedCount = 0
                }, ct);
                return;
            }

            // Parse JSON content
            var jsonContent = Encoding.UTF8.GetString(fileContent);
            var jsonEntries = JsonSerializer.Deserialize<List<Models.StreamingHistoryEntry>>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (jsonEntries == null || !jsonEntries.Any())
            {
                await SendOkAsync(new UploadDataResponse
                {
                    Success = true,
                    Message = "No entries found in file",
                    FileName = fileName
                }, ct);
                return;
            }

            // Create UploadedFile record first to get its ID
            var uploadedFile = new UploadedFile
            {
                UserId = userId,
                FileName = fileName,
                FileHash = fileHash,
                FileSize = file.Length,
                UploadedAt = DateTime.UtcNow
            };
            
            _context.UploadedFiles.Add(uploadedFile);
            await _context.SaveChangesAsync(ct);
            
            // Convert JSON entries to database entries with UploadedFileId
            var dbEntries = new List<StreamingHistoryEntry>();
            var totalProcessed = 0;
            
            foreach (var jsonEntry in jsonEntries)
            {
                try
                {
                    var dbEntry = ConvertJsonToDbEntry(userId, uploadedFile.Id, jsonEntry);
                    dbEntries.Add(dbEntry);
                    totalProcessed++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to convert entry for user {UserId}", userId);
                }
            }

            if (!dbEntries.Any())
            {
                // Delete the uploaded file record if no valid entries
                _context.UploadedFiles.Remove(uploadedFile);
                await _context.SaveChangesAsync(ct);
                
                await SendOkAsync(new UploadDataResponse
                {
                    Success = true,
                    Message = "No valid entries found in file",
                    FileName = fileName,
                    TotalProcessed = totalProcessed,
                    EntryCount = 0,
                    SkippedCount = 0
                }, ct);
                return;
            }

            // Disable change tracking for bulk inserts (significant performance improvement)
            var originalAutoDetectChanges = _context.ChangeTracker.AutoDetectChangesEnabled;
            _context.ChangeTracker.AutoDetectChangesEnabled = false;

            try
            {
                // Insert all entries with file reference
                _context.StreamingHistory.AddRange(dbEntries);
                await _context.SaveChangesAsync(ct);
                
                await SendOkAsync(new UploadDataResponse
                {
                    Success = true,
                    Message = $"Successfully saved {dbEntries.Count} entries to database",
                    FileName = fileName,
                    EntryCount = dbEntries.Count,
                    SkippedCount = 0,
                    TotalProcessed = totalProcessed
                }, ct);
            }
            finally
            {
                // Restore original change tracking setting
                _context.ChangeTracker.AutoDetectChangesEnabled = originalAutoDetectChanges;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload data for user {UserId}", userId);
            await SendOkAsync(new UploadDataResponse
            {
                Success = false,
                Message = $"An error occurred: {ex.Message}"
            }, ct);
        }
    }

    private StreamingHistoryEntry ConvertJsonToDbEntry(Guid userId, Guid uploadedFileId, Models.StreamingHistoryEntry jsonEntry)
    {
        var playedAt = DateTime.Parse(jsonEntry.Timestamp ?? DateTime.MinValue.ToString(), null, System.Globalization.DateTimeStyles.None);
        playedAt = DateTime.SpecifyKind(playedAt, DateTimeKind.Unspecified);

        var contentType = DetermineContentType(jsonEntry);
        var artistName = jsonEntry.UnifiedArtistName;
        var trackName = jsonEntry.UnifiedTrackName;

        return new StreamingHistoryEntry(
            userId: userId,
            playedAt: playedAt,
            msPlayed: jsonEntry.MsPlayed,
            platform: jsonEntry.Platform ?? "unknown",
            contentType: contentType,
            uploadedFileId: uploadedFileId,
            countryCode: jsonEntry.PlayedInCountryCode,
            spotifyUri: jsonEntry.SpotifyTrackUri ?? jsonEntry.SpotifyEpisodeUri,
            trackName: trackName,
            artistName: artistName,
            albumName: jsonEntry.MasterMetadataAlbumAlbumName,
            episodeName: jsonEntry.EpisodeName,
            showName: jsonEntry.EpisodeShowName,
            skipped: jsonEntry.Skipped,
            reasonStart: jsonEntry.ReasonStart,
            reasonEnd: jsonEntry.ReasonEnd,
            shuffle: jsonEntry.Shuffle,
            offline: jsonEntry.Offline
        );
    }

    private string DetermineContentType(Models.StreamingHistoryEntry jsonEntry)
    {
        if (!string.IsNullOrEmpty(jsonEntry.EpisodeName) || !string.IsNullOrEmpty(jsonEntry.EpisodeShowName))
            return "podcast";

        if (!string.IsNullOrEmpty(jsonEntry.SpotifyEpisodeUri))
            return "podcast";

        if (!string.IsNullOrEmpty(jsonEntry.SpotifyTrackUri))
            return "audio";

        return "audio";
    }

    private static string ComputeFileHash(byte[] fileContent)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(fileContent);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}


