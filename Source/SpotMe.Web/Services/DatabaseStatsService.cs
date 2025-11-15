using Microsoft.EntityFrameworkCore;
using SpotMe.Web.Models;
using SpotMe.Web.Persistency;

namespace SpotMe.Web.Services;

public class DatabaseStatsService
{
    private readonly DatabaseContext _context;
    private readonly UserDataService _userDataService;
    private readonly ILogger<DatabaseStatsService> _logger;

    public DatabaseStatsService(
        DatabaseContext context, 
        UserDataService userDataService,
        ILogger<DatabaseStatsService> logger)
    {
        _context = context;
        _userDataService = userDataService;
        _logger = logger;
    }

    /// <summary>
    /// Get stats overview using optimized database queries
    /// </summary>
    public async Task<StatsOverview> GetStatsOverviewAsync(DateOnly? startDate = null, DateOnly? endDate = null, Guid? userId = null)
    {
        if (userId == null)
        {
            userId = Guid.Parse(_userDataService.GetCurrentUserId());
        }
        
        // Build base query with date filtering
        var query = _context.StreamingHistory
            .Where(sh => sh.UserId == userId.Value);

        if (startDate.HasValue)
        {
            var startDateTime = startDate.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(sh => sh.PlayedAt >= startDateTime);
        }

        if (endDate.HasValue)
        {
            var endDateTime = endDate.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(sh => sh.PlayedAt <= endDateTime);
        }

        // Check if we have data
        var hasData = await query.AnyAsync();
        if (!hasData)
        {
            return new StatsOverview();
        }

        // Get date range
        var dateRange = await query
            .GroupBy(x => 1)
            .Select(g => new { 
                MinDate = g.Min(x => x.PlayedAt), 
                MaxDate = g.Max(x => x.PlayedAt) 
            })
            .FirstAsync();

        var actualStartDate = dateRange.MinDate;
        var actualEndDate = dateRange.MaxDate;
        var daysInRange = (actualEndDate - actualStartDate).Days + 1;

        // Basic stats - simplified queries to avoid PostgreSQL syntax issues
        var basicStats = await query
            .GroupBy(x => 1)
            .Select(g => new {
                TotalTracks = g.Count(),
                TotalMinutes = g.Sum(x => x.MsPlayed) / 60000.0
            })
            .FirstAsync();

        // Get unique counts separately to avoid complex nested queries
        var uniqueArtists = await query
            .Where(x => x.ArtistName != null)
            .Select(x => x.ArtistName)
            .Distinct()
            .CountAsync();

        var uniqueTracks = await query
            .Where(x => x.TrackName != null && x.ArtistName != null)
            .Select(x => new { x.ArtistName, x.TrackName })
            .Distinct()
            .CountAsync();

        var averageMinutesPerDay = daysInRange > 0 ? basicStats.TotalMinutes / daysInRange : 0;

        // Content type breakdown
        var contentTypeStats = await query
            .GroupBy(sh => sh.ContentType)
            .Select(g => new { Type = g.Key, Count = g.Count(), Minutes = g.Sum(x => x.MsPlayed) / 60000.0 })
            .ToListAsync();

        var contentTypeBreakdown = new ContentTypeBreakdown();
        foreach (var stat in contentTypeStats)
        {
            if (stat.Type == "audio")
            {
                contentTypeBreakdown.AudioTrackCount = stat.Count;
                contentTypeBreakdown.AudioTrackMinutes = stat.Minutes;
            }
            else if (stat.Type == "podcast")
            {
                contentTypeBreakdown.PodcastCount = stat.Count;
                contentTypeBreakdown.PodcastMinutes = stat.Minutes;
            }
            else if (stat.Type == "audiobook")
            {
                contentTypeBreakdown.AudiobookCount = stat.Count;
                contentTypeBreakdown.AudiobookMinutes = stat.Minutes;
            }
            else
            {
                contentTypeBreakdown.UnknownCount = stat.Count;
                contentTypeBreakdown.UnknownMinutes = stat.Minutes;
            }
        }

        // Platform breakdown
        var platformStats = await query
            .GroupBy(sh => sh.Platform)
            .Select(g => new { Platform = g.Key, Count = g.Count(), Minutes = g.Sum(x => x.MsPlayed) / 60000.0 })
            .ToListAsync();

        var platformBreakdown = new PlatformBreakdown();
        foreach (var stat in platformStats)
        {
            platformBreakdown.PlatformUsage[stat.Platform] = stat.Count;
            platformBreakdown.PlatformMinutes[stat.Platform] = stat.Minutes;
        }

        // Top artists - optimized query
        var topArtists = await query
            .Where(sh => sh.ArtistName != null)
            .GroupBy(sh => sh.ArtistName)
            .Select(g => new ArtistStats
            {
                ArtistName = g.Key!,
                PlayCount = g.Count(),
                TotalMinutes = g.Sum(x => x.MsPlayed) / 60000.0,
                UniqueTracks = g.Select(x => x.TrackName).Distinct().Count(),
                UniqueAlbums = g.Where(x => x.AlbumName != null).Select(x => x.AlbumName).Distinct().Count(),
                PrimaryContentType = g.Select(x => x.ContentType).First() == "audiobook" ? ContentType.Audiobook :
                                    g.Select(x => x.ContentType).First() == "audio" ? ContentType.AudioTrack : 
                                    g.Select(x => x.ContentType).First() == "podcast" ? ContentType.Podcast : ContentType.Unknown
            })
            .OrderByDescending(a => a.TotalMinutes)
            .Take(10)
            .ToListAsync();

        // Top tracks - optimized query
        var topTracks = await query
            .Where(sh => sh.TrackName != null && sh.ArtistName != null)
            .GroupBy(sh => new { sh.ArtistName, sh.TrackName })
            .Select(g => new TrackStats
            {
                ArtistName = g.Key.ArtistName!,
                TrackName = g.Key.TrackName!,
                AlbumName = g.Select(x => x.AlbumName).FirstOrDefault(),
                SpotifyUri = g.Select(x => x.SpotifyUri).FirstOrDefault(),
                PlayCount = g.Count(),
                TotalMinutes = g.Sum(x => x.MsPlayed) / 60000.0,
                ContentType = g.Select(x => x.ContentType).First() == "audiobook" ? ContentType.Audiobook :
                             g.Select(x => x.ContentType).First() == "audio" ? ContentType.AudioTrack : 
                             g.Select(x => x.ContentType).First() == "podcast" ? ContentType.Podcast : ContentType.Unknown,
                AveragePlayDuration = g.Average(x => x.MsPlayed) / 60000.0,
                MostCommonCompletionStatus = PlaybackCompletionStatus.Unknown // Would need to add this field to schema
            })
            .OrderByDescending(t => t.TotalMinutes)
            .Take(10)
            .ToListAsync();

        // Top albums - optimized query
        var topAlbums = await query
            .Where(sh => sh.AlbumName != null && sh.ArtistName != null)
            .GroupBy(sh => new { sh.ArtistName, sh.AlbumName })
            .Select(g => new AlbumStats
            {
                ArtistName = g.Key.ArtistName!,
                AlbumName = g.Key.AlbumName!,
                PlayCount = g.Count(),
                TotalMinutes = g.Sum(x => x.MsPlayed) / 60000.0,
                UniqueTracks = g.Select(x => x.TrackName).Distinct().Count()
            })
            .OrderByDescending(a => a.TotalMinutes)
            .Take(10)
            .ToListAsync();

        // Time-based stats calculated in application
        var hourlyStats = await query
            .GroupBy(sh => sh.PlayedAt.Hour)
            .Select(g => new { Hour = g.Key, Minutes = g.Sum(x => x.MsPlayed) / 60000.0 })
            .OrderBy(x => x.Hour)
            .ToListAsync();

        var dailyStats = await query
            .GroupBy(sh => new { Year = sh.PlayedAt.Year, Month = sh.PlayedAt.Month, Day = sh.PlayedAt.Day })
            .Select(g => new { 
                Date = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day),
                Minutes = g.Sum(x => x.MsPlayed) / 60000.0 
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        var monthlyStats = await query
            .GroupBy(sh => new { Year = sh.PlayedAt.Year, Month = sh.PlayedAt.Month })
            .Select(g => new { 
                Year = g.Key.Year,
                Month = g.Key.Month,
                Minutes = g.Sum(x => x.MsPlayed) / 60000.0 
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToListAsync();

        // Create time-based stats
        var timeBasedStats = new TimeBasedStats
        {
            HourOfDayStats = hourlyStats.Select(x => new HourOfDayStats
            {
                Hour = x.Hour,
                HourLabel = $"{x.Hour:00}:00",
                TotalMinutes = x.Minutes,
                PlayCount = 0, // Would need additional query
                AverageMinutesPerOccurrence = 0
            }).ToList(),
            MonthlyStats = monthlyStats.Select(x => new MonthlyStats
            {
                MonthYearLabel = $"{x.Year:0000}-{x.Month:00}",
                TotalMinutes = x.Minutes,
                PlayCount = 0, // Would need additional query
                AverageMinutesPerDay = 0,
                Month = x.Month,
                Year = x.Year,
                MonthName = ""
            }).ToList()
        };

        return new StatsOverview
        {
            TotalTracks = basicStats.TotalTracks,
            TotalMinutes = basicStats.TotalMinutes,
            UniqueArtists = uniqueArtists,
            UniqueTracks = uniqueTracks,
            AverageMinutesPerDay = averageMinutesPerDay,
            StartDate = actualStartDate,
            EndDate = actualEndDate,
            ContentTypeBreakdown = contentTypeBreakdown,
            PlatformBreakdown = platformBreakdown,
            TopArtists = topArtists,
            TopTracks = topTracks,
            TopAlbums = topAlbums,
            TimeBasedStats = timeBasedStats,
            PlaybackBehavior = new PlaybackBehavior()
        };
    }

    /// <summary>
    /// Check if user has data imported to database
    /// </summary>
    public async Task<bool> HasImportedDataAsync(Guid? userId = null)
    {
        if (userId == null)
        {
            userId = Guid.Parse(_userDataService.GetCurrentUserId());
        }
        return await _context.StreamingHistory.AnyAsync(sh => sh.UserId == userId.Value);
    }

    /// <summary>
    /// Get count of imported entries for current user
    /// </summary>
    public async Task<int> GetImportedEntryCountAsync(Guid? userId = null)
    {
        if (userId == null)
        {
            userId = Guid.Parse(_userDataService.GetCurrentUserId());
        }
        return await _context.StreamingHistory.CountAsync(sh => sh.UserId == userId.Value);
    }
}
