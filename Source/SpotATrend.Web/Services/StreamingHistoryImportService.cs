using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SpotATrend.Web.Models;
using SpotATrend.Web.Persistency;
using SpotATrend.Web.Domain;
using System.Security.Cryptography;

namespace SpotATrend.Web.Services;

public class StreamingHistoryImportService
{
    private readonly DatabaseContext _context;
    private readonly ILogger<StreamingHistoryImportService> _logger;
    private readonly UserDataService _userDataService;

    public StreamingHistoryImportService(
        DatabaseContext context, 
        ILogger<StreamingHistoryImportService> logger,
        UserDataService userDataService)
    {
        _context = context;
        _logger = logger;
        _userDataService = userDataService;
    }

    /// <summary>
    /// Import streaming history from JSON files to database for a specific user
    /// </summary>
    public async Task<ImportResult> ImportUserStreamingHistoryAsync(string userId)
    {
        var result = new ImportResult { UserId = userId };
        var userGuid = Guid.Parse(userId);

        try
        {
            // Check if user has already imported data
            var existingCount = await _context.StreamingHistory
                .Where(sh => sh.UserId == userGuid)
                .CountAsync();

            if (existingCount > 0)
            {
                _logger.LogInformation("User {UserId} already has {Count} entries imported", userId, existingCount);
                result.AlreadyImported = true;
                result.ExistingEntryCount = existingCount;
                return result;
            }

            // For now, simulate empty data since we removed JSON loading
            // TODO: Implement proper JSON file upload and parsing
            var jsonEntries = new List<Models.StreamingHistoryEntry>();
            
            if (!jsonEntries.Any())
            {
                _logger.LogWarning("No streaming history data found for user {UserId}", userId);
                result.Success = true;
                return result;
            }

            _logger.LogInformation("Converting {Count} JSON entries to database format for user {UserId}", 
                jsonEntries.Count, userId);

            // Create a placeholder UploadedFile record for legacy import
            // Note: This service should be updated to accept a file parameter
            var uploadedFile = new UploadedFile
            {
                UserId = userGuid,
                FileName = "Legacy Import",
                FileHash = Guid.NewGuid().ToString(), // Placeholder hash
                FileSize = 0,
                UploadedAt = DateTime.UtcNow
            };
            
            _context.UploadedFiles.Add(uploadedFile);
            await _context.SaveChangesAsync();

            var dbEntries = new List<StreamingHistoryEntry>();
            var batchSize = 10000; // Increased batch size for better performance
            var processed = 0;

            // Disable change tracking for bulk inserts (significant performance improvement)
            var originalAutoDetectChanges = _context.ChangeTracker.AutoDetectChangesEnabled;
            _context.ChangeTracker.AutoDetectChangesEnabled = false;

            try
            {
                foreach (var jsonEntry in jsonEntries)
                {
                    try
                    {
                        var dbEntry = ConvertJsonToDbEntry(userGuid, uploadedFile.Id, jsonEntry);
                        dbEntries.Add(dbEntry);
                        processed++;

                        // Batch insert for performance
                        if (dbEntries.Count >= batchSize)
                        {
                            await InsertBatchAsync(dbEntries);
                            result.ImportedCount += dbEntries.Count;
                            dbEntries.Clear();
                            
                            _logger.LogDebug("Imported batch of {BatchSize} entries for user {UserId}. Total: {Total}", 
                                batchSize, userId, result.ImportedCount);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to convert entry {Index} for user {UserId}", processed, userId);
                        result.ErrorCount++;
                    }
                }

                // Insert remaining entries
                if (dbEntries.Any())
                {
                    await InsertBatchAsync(dbEntries);
                    result.ImportedCount += dbEntries.Count;
                }
            }
            finally
            {
                // Restore original change tracking setting
                _context.ChangeTracker.AutoDetectChangesEnabled = originalAutoDetectChanges;
            }

            result.Success = true;
            _logger.LogInformation("Successfully imported {ImportedCount} entries for user {UserId} with {ErrorCount} errors", 
                result.ImportedCount, userId, result.ErrorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import streaming history for user {UserId}", userId);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Convert JSON streaming entry to database entity
    /// </summary>
    private StreamingHistoryEntry ConvertJsonToDbEntry(Guid userId, Guid uploadedFileId, Models.StreamingHistoryEntry jsonEntry)
    {
        // Parse timestamp from Spotify JSON 
        // Note: Despite the 'Z' suffix, Spotify appears to store local times in UTC format
        var playedAt = DateTime.Parse(jsonEntry.Timestamp ?? DateTime.MinValue.ToString(), null, System.Globalization.DateTimeStyles.RoundtripKind);
        
        // Determine content type
        var contentType = DetermineContentType(jsonEntry);
        
        // Get unified names
        var artistName = jsonEntry.UnifiedArtistName;
        var trackName = jsonEntry.UnifiedTrackName;
        
        // Create database entry
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

    /// <summary>
    /// Determine content type from JSON entry
    /// </summary>
    private string DetermineContentType(Models.StreamingHistoryEntry jsonEntry)
    {
        // Check for audiobook first
        if (!string.IsNullOrEmpty(jsonEntry.AudiobookTitle) || !string.IsNullOrEmpty(jsonEntry.AudiobookUri))
            return "audiobook";
        
        if (!string.IsNullOrEmpty(jsonEntry.EpisodeName) || !string.IsNullOrEmpty(jsonEntry.EpisodeShowName))
            return "podcast";
        
        if (!string.IsNullOrEmpty(jsonEntry.SpotifyEpisodeUri))
            return "podcast";
            
        if (!string.IsNullOrEmpty(jsonEntry.SpotifyTrackUri))
            return "audio";
            
        // Default to audio for music content
        return "audio";
    }

    /// <summary>
    /// Insert batch of entries with optimized performance
    /// </summary>
    private async Task InsertBatchAsync(List<StreamingHistoryEntry> entries)
    {
        _context.StreamingHistory.AddRange(entries);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Get import statistics for a user
    /// </summary>
    public async Task<ImportStats> GetImportStatsAsync(string userId)
    {
        var userGuid = Guid.Parse(userId);
        
        var dbCount = await _context.StreamingHistory
            .Where(sh => sh.UserId == userGuid)
            .CountAsync();
            
        var jsonCount = 0; // No JSON loading for now
        
        return new ImportStats
        {
            UserId = userId,
            JsonEntryCount = jsonCount,
            DatabaseEntryCount = dbCount,
            IsImported = dbCount > 0,
            ImportPercentage = jsonCount > 0 ? (double)dbCount / jsonCount * 100 : 0
        };
    }
}

public class ImportResult
{
    public string UserId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public bool AlreadyImported { get; set; }
    public int ImportedCount { get; set; }
    public int ErrorCount { get; set; }
    public int ExistingEntryCount { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ImportStats
{
    public string UserId { get; set; } = string.Empty;
    public int JsonEntryCount { get; set; }
    public int DatabaseEntryCount { get; set; }
    public bool IsImported { get; set; }
    public double ImportPercentage { get; set; }
}
