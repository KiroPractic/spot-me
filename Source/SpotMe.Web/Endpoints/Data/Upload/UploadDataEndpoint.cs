using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using SpotMe.Web.Persistency;
using SpotMe.Web.Services;
using System.Security.Claims;
using System.Text.Json;

namespace SpotMe.Web.Endpoints.Data.Upload;

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

            // Parse JSON content
            var jsonContent = System.Text.Encoding.UTF8.GetString(fileContent);
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

            // Check if user already has data
            var existingCount = await _context.StreamingHistory
                .Where(sh => sh.UserId == userId)
                .CountAsync(ct);

            if (existingCount > 0)
            {
                await SendOkAsync(new UploadDataResponse
                {
                    Success = true,
                    Message = $"User already has {existingCount} entries in database",
                    FileName = fileName,
                    EntryCount = existingCount
                }, ct);
                return;
            }

            // Convert and save to database
            var dbEntries = new List<StreamingHistoryEntry>();
            var batchSize = 1000;
            var savedCount = 0;

            foreach (var jsonEntry in jsonEntries)
            {
                try
                {
                    var dbEntry = ConvertJsonToDbEntry(userId, jsonEntry);
                    dbEntries.Add(dbEntry);

                    if (dbEntries.Count >= batchSize)
                    {
                        _context.StreamingHistory.AddRange(dbEntries);
                        await _context.SaveChangesAsync(ct);
                        savedCount += dbEntries.Count;
                        dbEntries.Clear();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to convert entry for user {UserId}", userId);
                }
            }

            if (dbEntries.Any())
            {
                _context.StreamingHistory.AddRange(dbEntries);
                await _context.SaveChangesAsync(ct);
                savedCount += dbEntries.Count;
            }

            await SendOkAsync(new UploadDataResponse
            {
                Success = true,
                Message = $"Successfully saved {savedCount} entries to database",
                FileName = fileName,
                EntryCount = savedCount
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload data for user {UserId}", userId);
            await SendOkAsync(new UploadDataResponse
            {
                Success = false,
                Message = ex.Message
            }, ct);
        }
    }

    private StreamingHistoryEntry ConvertJsonToDbEntry(Guid userId, Models.StreamingHistoryEntry jsonEntry)
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
}

