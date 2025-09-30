using SpotMe.Web.Services;
using SpotMe.Web.Models;
using Microsoft.AspNetCore.Components.Forms;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SpotMe.Web.Persistency;

namespace SpotMe.Web.Services;

public class UserDataService
{
    private readonly CustomAuthenticationService _authService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<UserDataService> _logger;
    private readonly DatabaseContext _context;

    public UserDataService(CustomAuthenticationService authService, IWebHostEnvironment environment, ILogger<UserDataService> logger, DatabaseContext context)
    {
        _authService = authService;
        _environment = environment;
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// Get the current authenticated user ID
    /// </summary>
    public string GetCurrentUserId()
    {
        var user = _authService.CurrentUser;
        if (!user.Identity?.IsAuthenticated == true)
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User ID not found in claims");
        }

        return userId;
    }

    /// <summary>
    /// Get the base user data directory path
    /// </summary>
    public string GetUserDataBasePath()
    {
        return Path.Combine(_environment.ContentRootPath, "UserData");
    }

    /// <summary>
    /// Get user-specific data directory path
    /// </summary>
    public string GetUserDataPath(string userId)
    {
        return Path.Combine(GetUserDataBasePath(), userId);
    }

    /// <summary>
    /// Get user-specific Spotify data directory path
    /// </summary>
    public string GetUserSpotifyDataPath(string userId)
    {
        return Path.Combine(GetUserDataPath(userId), "SpotifyStats");
    }

    /// <summary>
    /// Ensure user directory structure exists
    /// </summary>
    public Task EnsureUserDirectoryExistsAsync(string userId)
    {
        var userPath = GetUserDataPath(userId);
        var spotifyPath = GetUserSpotifyDataPath(userId);

        if (!Directory.Exists(userPath))
        {
            Directory.CreateDirectory(userPath);
            _logger.LogInformation("Created user directory: {Path}", userPath);
        }

        if (!Directory.Exists(spotifyPath))
        {
            Directory.CreateDirectory(spotifyPath);
            _logger.LogInformation("Created user Spotify data directory: {Path}", spotifyPath);
        }
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Save Spotify streaming history data to database from uploaded JSON file
    /// </summary>
    public async Task<SaveResult> SaveSpotifyDataToDatabaseAsync(string userId, IBrowserFile file)
    {
        if (file == null || file.Size == 0)
            throw new ArgumentException("File is empty", nameof(file));

        if (!file.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("File must be a JSON file", nameof(file));

        // File size validation (reasonable limits)
        const long maxFileSize = 100 * 1024 * 1024; // 100MB
        if (file.Size > maxFileSize)
            throw new ArgumentException($"File size ({FormatFileSize(file.Size)}) exceeds maximum allowed size ({FormatFileSize(maxFileSize)})", nameof(file));

        var result = new SaveResult { UserId = userId, FileName = file.Name };
        var userGuid = Guid.Parse(userId);

        try
        {
            // Read and validate file content
            byte[] fileContent;
            using (var stream = file.OpenReadStream(maxFileSize))
            {
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                fileContent = memoryStream.ToArray();
            }

            // Validate file content
            using (var validationStream = new MemoryStream(fileContent))
            {
                var validationResult = await ValidateSpotifyJsonFileAsync(validationStream, file.Name);
                
                if (!validationResult.IsSuccess)
                {
                    throw new ArgumentException($"File validation failed: {validationResult.Message}", nameof(file));
                }
            }

            // Parse JSON content
            var jsonContent = System.Text.Encoding.UTF8.GetString(fileContent);
            var jsonEntries = JsonSerializer.Deserialize<List<Models.StreamingHistoryEntry>>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (jsonEntries == null || !jsonEntries.Any())
            {
                result.Success = true;
                result.Message = "No entries found in file";
                return result;
            }

            // Check if user already has data
            var existingCount = await _context.StreamingHistory
                .Where(sh => sh.UserId == userGuid)
                .CountAsync();

            if (existingCount > 0)
            {
                result.Success = true;
                result.AlreadyExists = true;
                result.ExistingEntryCount = existingCount;
                result.Message = $"User already has {existingCount} entries in database";
                return result;
            }

            // Convert and save to database
            var dbEntries = new List<StreamingHistoryEntry>();
            var batchSize = 1000;
            var processed = 0;

            foreach (var jsonEntry in jsonEntries)
            {
                try
                {
                    var dbEntry = ConvertJsonToDbEntry(userGuid, jsonEntry);
                    dbEntries.Add(dbEntry);
                    processed++;

                    // Batch insert for performance
                    if (dbEntries.Count >= batchSize)
                    {
                        _context.StreamingHistory.AddRange(dbEntries);
                        await _context.SaveChangesAsync();
                        result.SavedCount += dbEntries.Count;
                        dbEntries.Clear();
                        
                        _logger.LogDebug("Saved batch of {BatchSize} entries for user {UserId}. Total: {Total}", 
                            batchSize, userId, result.SavedCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to convert entry {Index} for user {UserId}", processed, userId);
                    result.ErrorCount++;
                }
            }

            // Save remaining entries
            if (dbEntries.Any())
            {
                _context.StreamingHistory.AddRange(dbEntries);
                await _context.SaveChangesAsync();
                result.SavedCount += dbEntries.Count;
            }

            result.Success = true;
            result.Message = $"Successfully saved {result.SavedCount} entries to database";
            
            _logger.LogInformation("Saved Spotify data to database for user {UserId}: {SavedCount} entries saved, {ErrorCount} errors", 
                userId, result.SavedCount, result.ErrorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save Spotify data to database for user {UserId}", userId);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Validate that a JSON file contains Spotify streaming history data
    /// </summary>
    private async Task<ValidationResult> ValidateSpotifyJsonFileAsync(Stream fileStream, string fileName)
    {
        try
        {
            // Read content without seeking (stream position should be at beginning)
            using var reader = new StreamReader(fileStream, leaveOpen: true);
            var content = await reader.ReadToEndAsync();
            
            // Basic JSON validation
            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(content);
            }
            catch (JsonException ex)
            {
                return ValidationResult.Failure($"Invalid JSON format: {ex.Message}");
            }

            // Check if it's an array
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return ValidationResult.Failure("File must contain a JSON array of streaming entries");
            }

            var array = document.RootElement;
            
            // Check if array is empty
            if (array.GetArrayLength() == 0)
            {
                return ValidationResult.Warning("File appears to be empty (no streaming entries found)");
            }

            // Validate first few entries to ensure they have expected Spotify structure
            var validEntries = 0;
            var maxToCheck = Math.Min(5, array.GetArrayLength());
            
            for (int i = 0; i < maxToCheck; i++)
            {
                var entry = array[i];
                
                // Check for required Spotify streaming history fields
                bool hasRequiredFields = entry.TryGetProperty("ts", out _) || // timestamp
                                        entry.TryGetProperty("endTime", out _) || // end time
                                        entry.TryGetProperty("artistName", out _) || // artist
                                        entry.TryGetProperty("trackName", out _) || // track
                                        entry.TryGetProperty("master_metadata_track_name", out _) || // extended format
                                        entry.TryGetProperty("master_metadata_album_artist_name", out _); // extended format

                if (hasRequiredFields)
                {
                    validEntries++;
                }
            }

            if (validEntries == 0)
            {
                return ValidationResult.Failure("File does not appear to contain valid Spotify streaming history data. Expected fields like 'ts', 'artistName', 'trackName' or similar.");
            }

            // Additional filename validation for Spotify format
            var isValidSpotifyFilename = fileName.Contains("Streaming_History", StringComparison.OrdinalIgnoreCase) ||
                                       fileName.Contains("spotify", StringComparison.OrdinalIgnoreCase) ||
                                       fileName.Contains("streaming", StringComparison.OrdinalIgnoreCase);

            if (!isValidSpotifyFilename)
            {
                return ValidationResult.Warning($"Filename '{fileName}' doesn't match typical Spotify data format, but content appears valid.");
            }

            return ValidationResult.Success($"Valid Spotify streaming history file with {array.GetArrayLength()} entries");
        }
        catch (Exception ex)
        {
            return ValidationResult.Failure($"Error validating file: {ex.Message}");
        }
    }

    /// <summary>
    /// Convert JSON streaming entry to database entity
    /// </summary>
    private StreamingHistoryEntry ConvertJsonToDbEntry(Guid userId, Models.StreamingHistoryEntry jsonEntry)
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
    /// Get user's streaming history from database
    /// </summary>
    public async Task<List<StreamingHistoryEntry>> GetUserStreamingHistoryAsync(string userId, int skip = 0, int take = 100)
    {
        var userGuid = Guid.Parse(userId);
        
        return await _context.StreamingHistory
            .Where(sh => sh.UserId == userGuid)
            .OrderByDescending(sh => sh.PlayedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    /// <summary>
    /// Get count of streaming history entries for a user
    /// </summary>
    public async Task<int> GetUserStreamingHistoryCountAsync(string userId)
    {
        var userGuid = Guid.Parse(userId);
        
        return await _context.StreamingHistory
            .Where(sh => sh.UserId == userGuid)
            .CountAsync();
    }

    /// <summary>
    /// Delete all streaming history data for a user
    /// </summary>
    public async Task<bool> DeleteUserStreamingHistoryAsync(string userId)
    {
        var userGuid = Guid.Parse(userId);
        
        var entries = await _context.StreamingHistory
            .Where(sh => sh.UserId == userGuid)
            .ToListAsync();
            
        if (!entries.Any())
        {
            return false;
        }
        
        _context.StreamingHistory.RemoveRange(entries);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Deleted {Count} streaming history entries for user {UserId}", entries.Count, userId);
        return true;
    }

    /// <summary>
    /// Get list of uploaded files for a user (now returns database info instead of file info)
    /// </summary>
    public async Task<List<UserDataFile>> GetUserFilesAsync(string userId)
    {
        var userGuid = Guid.Parse(userId);
        
        // Get entry count from database
        var entryCount = await _context.StreamingHistory
            .Where(sh => sh.UserId == userGuid)
            .CountAsync();
            
        if (entryCount == 0)
        {
            return new List<UserDataFile>();
        }
        
        // Get the earliest and latest play dates for metadata
        var dateRange = await _context.StreamingHistory
            .Where(sh => sh.UserId == userGuid)
            .Select(sh => new { sh.PlayedAt })
            .OrderBy(sh => sh.PlayedAt)
            .ToListAsync();
            
        var earliestDate = dateRange.FirstOrDefault()?.PlayedAt ?? DateTime.Now;
        var latestDate = dateRange.LastOrDefault()?.PlayedAt ?? DateTime.Now;
        
        // Create a virtual "file" representing the database data
        var files = new List<UserDataFile>
        {
            new UserDataFile
            {
                FileName = "Streaming History Database",
                FileSize = 0, // Database size is not easily calculable
                UploadedAt = earliestDate, // Use earliest play date as "upload" date
                EntryCount = entryCount,
                IsDatabaseEntry = true,
                DateRange = $"{earliestDate:yyyy-MM-dd} to {latestDate:yyyy-MM-dd}"
            }
        };
        
        return files;
    }

    /// <summary>
    /// Delete streaming history data for a user (now deletes from database instead of file)
    /// </summary>
    public async Task<bool> DeleteUserFileAsync(string userId, string fileName)
    {
        // Since we're now using database, we delete all user data regardless of "filename"
        return await DeleteUserStreamingHistoryAsync(userId);
    }

    /// <summary>
    /// Get a unique file path if the file already exists
    /// </summary>
    private string GetUniqueFilePath(string originalPath)
    {
        if (!File.Exists(originalPath))
            return originalPath;

        var directory = Path.GetDirectoryName(originalPath);
        var nameWithoutExt = Path.GetFileNameWithoutExtension(originalPath);
        var extension = Path.GetExtension(originalPath);

        var counter = 1;
        string newPath;
        do
        {
            var newName = $"{nameWithoutExt}_{counter}{extension}";
            newPath = Path.Combine(directory!, newName);
            counter++;
        } while (File.Exists(newPath));

        return newPath;
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

/// <summary>
/// Represents a user's data file (now supports both file and database entries)
/// </summary>
public class UserDataFile
{
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    public int EntryCount { get; set; }
    public bool IsDatabaseEntry { get; set; } = false;
    public string? DateRange { get; set; }
    
    public string FileSizeFormatted => IsDatabaseEntry ? "Database Entry" : FormatFileSize(FileSize);
    
    public static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

/// <summary>
/// Result of saving Spotify data to database
/// </summary>
public class SaveResult
{
    public string UserId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public bool AlreadyExists { get; set; }
    public int SavedCount { get; set; }
    public int ErrorCount { get; set; }
    public int ExistingEntryCount { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Validation result for file uploads
/// </summary>
public class ValidationResult
{
    public bool IsSuccess { get; private set; }
    public bool IsWarning { get; private set; }
    public string Message { get; private set; } = string.Empty;

    private ValidationResult(bool isSuccess, bool isWarning, string message)
    {
        IsSuccess = isSuccess;
        IsWarning = isWarning;
        Message = message;
    }

    public static ValidationResult Success(string message) => new(true, false, message);
    public static ValidationResult Warning(string message) => new(true, true, message);
    public static ValidationResult Failure(string message) => new(false, false, message);
}