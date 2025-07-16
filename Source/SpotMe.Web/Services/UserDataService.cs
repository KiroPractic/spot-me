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
    /// Save a Spotify data file uploaded via browser
    /// </summary>
    public async Task SaveSpotifyDataFileAsync(string userId, IBrowserFile file)
    {
        if (file == null || file.Size == 0)
            throw new ArgumentException("File is empty", nameof(file));

        if (!file.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("File must be a JSON file", nameof(file));

        await EnsureUserDirectoryExistsAsync(userId);

        var filePath = Path.Combine(GetUserSpotifyDataPath(userId), file.Name);

        using var stream = new FileStream(filePath, FileMode.Create);
        await file.OpenReadStream().CopyToAsync(stream);

        _logger.LogInformation("Uploaded Spotify data file for user {UserId}: {FileName} ({Size} bytes)", 
            userId, file.Name, file.Size);
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