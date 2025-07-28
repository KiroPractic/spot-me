using SpotMe.Web.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;

namespace SpotMe.Web.Services;

public class UserDataService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<UserDataService> _logger;

    public UserDataService(IWebHostEnvironment environment, ILogger<UserDataService> logger)
    {
        _environment = environment;
        _logger = logger;
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
    public async Task EnsureUserDirectoryExistsAsync(string userId)
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
    }

    /// <summary>
    /// Save user's Spotify streaming history JSON file
    /// </summary>
    public async Task SaveSpotifyDataFileAsync(string userId, string fileName, string jsonContent)
    {
        await EnsureUserDirectoryExistsAsync(userId);
        
        var filePath = Path.Combine(GetUserSpotifyDataPath(userId), fileName);
        
        // Validate JSON content
        try
        {
            JsonDocument.Parse(jsonContent);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Invalid JSON content: {ex.Message}", nameof(jsonContent));
        }

        await File.WriteAllTextAsync(filePath, jsonContent);
        _logger.LogInformation("Saved Spotify data file for user {UserId}: {FileName}", userId, fileName);
    }

    /// <summary>
    /// Save user's Spotify streaming history from uploaded file
    /// </summary>
    public async Task SaveSpotifyDataFileAsync(string userId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty", nameof(file));

        if (!file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("File must be a JSON file", nameof(file));

        await EnsureUserDirectoryExistsAsync(userId);

        var filePath = Path.Combine(GetUserSpotifyDataPath(userId), file.FileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        _logger.LogInformation("Uploaded Spotify data file for user {UserId}: {FileName} ({Size} bytes)", 
            userId, file.FileName, file.Length);
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
    /// Save a Spotify data file uploaded via browser with enhanced validation
    /// </summary>
    public async Task SaveSpotifyDataFileAsync(string userId, IBrowserFile file)
    {
        if (file == null || file.Size == 0)
            throw new ArgumentException("File is empty", nameof(file));

        if (!file.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("File must be a JSON file", nameof(file));

        // File size validation (reasonable limits)
        const long maxFileSize = 100 * 1024 * 1024; // 100MB
        if (file.Size > maxFileSize)
            throw new ArgumentException($"File size ({FormatFileSize(file.Size)}) exceeds maximum allowed size ({FormatFileSize(maxFileSize)})", nameof(file));

        await EnsureUserDirectoryExistsAsync(userId);

        // Read the file content once to avoid seeking issues
        byte[] fileContent;
        using (var stream = file.OpenReadStream(maxFileSize))
        {
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            fileContent = memoryStream.ToArray();
        }

        // Validate file content using a new MemoryStream
        using (var validationStream = new MemoryStream(fileContent))
        {
            var validationResult = await ValidateSpotifyJsonFileAsync(validationStream, file.Name);
            
            if (!validationResult.IsSuccess)
            {
                throw new ArgumentException($"File validation failed: {validationResult.Message}", nameof(file));
            }
        }

        // Save the file
        var filePath = Path.Combine(GetUserSpotifyDataPath(userId), file.Name);
        
        // Check if file already exists and create unique name if needed
        var finalFilePath = GetUniqueFilePath(filePath);
        
        // Write the content to file
        await File.WriteAllBytesAsync(finalFilePath, fileContent);

        _logger.LogInformation("Uploaded Spotify data file for user {UserId}: {FileName} ({Size} bytes)", 
            userId, Path.GetFileName(finalFilePath), file.Size);
    }

    /// <summary>
    /// Load all streaming history for a specific user
    /// </summary>
    public async Task<List<StreamingHistoryEntry>> LoadUserStreamingHistoryAsync(string userId)
    {
        var spotifyPath = GetUserSpotifyDataPath(userId);
        
        if (!Directory.Exists(spotifyPath))
        {
            _logger.LogWarning("Spotify data directory not found for user {UserId}", userId);
            return new List<StreamingHistoryEntry>();
        }

        var entries = new List<StreamingHistoryEntry>();
        
        // Load all JSON files in the user's Spotify directory
        var jsonFiles = Directory.GetFiles(spotifyPath, "*.json");
        
        foreach (var file in jsonFiles)
        {
            try
            {
                var jsonContent = await File.ReadAllTextAsync(file);
                var fileEntries = JsonSerializer.Deserialize<List<StreamingHistoryEntry>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (fileEntries != null)
                {
                    entries.AddRange(fileEntries);
                    _logger.LogDebug("Loaded {Count} entries from {FileName} for user {UserId}", 
                        fileEntries.Count, Path.GetFileName(file), userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading file {FileName} for user {UserId}", file, userId);
            }
        }
        
        // Sort by timestamp
        return entries.OrderBy(e => e.StartDateTime).ToList();
    }

    /// <summary>
    /// Get list of uploaded files for a user
    /// </summary>
    public async Task<List<UserDataFile>> GetUserFilesAsync(string userId)
    {
        var spotifyPath = GetUserSpotifyDataPath(userId);
        
        if (!Directory.Exists(spotifyPath))
        {
            return new List<UserDataFile>();
        }

        var files = new List<UserDataFile>();
        var jsonFiles = Directory.GetFiles(spotifyPath, "*.json");
        
        foreach (var filePath in jsonFiles)
        {
            var fileInfo = new FileInfo(filePath);
            var fileName = fileInfo.Name;
            
            // Try to get entry count from the file
            var entryCount = 0;
            try
            {
                var jsonContent = await File.ReadAllTextAsync(filePath);
                var entries = JsonSerializer.Deserialize<List<StreamingHistoryEntry>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                entryCount = entries?.Count ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not parse file {FileName} for user {UserId}", fileName, userId);
            }
            
            files.Add(new UserDataFile
            {
                FileName = fileName,
                FileSize = fileInfo.Length,
                UploadedAt = fileInfo.CreationTime,
                EntryCount = entryCount
            });
        }
        
        return files.OrderByDescending(f => f.UploadedAt).ToList();
    }

    /// <summary>
    /// Delete a user's data file
    /// </summary>
    public async Task DeleteUserFileAsync(string userId, string fileName)
    {
        var filePath = Path.Combine(GetUserSpotifyDataPath(userId), fileName);
        
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger.LogInformation("Deleted file {FileName} for user {UserId}", fileName, userId);
        }
    }

    /// <summary>
    /// Get storage statistics for a user
    /// </summary>
    public async Task<UserStorageStats> GetUserStorageStatsAsync(string userId)
    {
        var spotifyPath = GetUserSpotifyDataPath(userId);
        
        if (!Directory.Exists(spotifyPath))
        {
            return new UserStorageStats
            {
                UserId = userId,
                TotalFiles = 0,
                TotalSizeBytes = 0,
                TotalEntries = 0
            };
        }

        var files = Directory.GetFiles(spotifyPath, "*.json");
        long totalSize = 0;
        int totalEntries = 0;

        foreach (var file in files)
        {
            var fileInfo = new FileInfo(file);
            totalSize += fileInfo.Length;
            
            try
            {
                var jsonContent = await File.ReadAllTextAsync(file);
                var entries = JsonSerializer.Deserialize<List<StreamingHistoryEntry>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                totalEntries += entries?.Count ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not parse file {FileName} for stats", file);
            }
        }

        return new UserStorageStats
        {
            UserId = userId,
            TotalFiles = files.Length,
            TotalSizeBytes = totalSize,
            TotalEntries = totalEntries
        };
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
/// Represents a user's data file
/// </summary>
public class UserDataFile
{
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    public int EntryCount { get; set; }
    
    public string FileSizeFormatted => FormatFileSize(FileSize);
    
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
/// User storage statistics
/// </summary>
public class UserStorageStats
{
    public string UserId { get; set; } = string.Empty;
    public int TotalFiles { get; set; }
    public long TotalSizeBytes { get; set; }
    public int TotalEntries { get; set; }
    
    public string TotalSizeFormatted => UserDataFile.FormatFileSize(TotalSizeBytes);
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