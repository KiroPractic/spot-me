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

        // Removed TopArtists, TopTracks, TopAlbums from root level
        // Music stats are in MusicStats.TopMusicArtists/Tracks/Albums
        // Podcasts are in TopPodcasts

        // Time-based stats calculated in application
        var hourlyStats = await query
            .GroupBy(sh => sh.PlayedAt.Hour)
            .Select(g => new { Hour = g.Key, Count = g.Count(), Minutes = g.Sum(x => x.MsPlayed) / 60000.0 })
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

        // Monthly stats grouped by month only (aggregating across all years)
        var monthlyStatsByMonth = await query
            .GroupBy(sh => sh.PlayedAt.Month)
            .Select(g => new { 
                Month = g.Key,
                Count = g.Count(),
                Minutes = g.Sum(x => x.MsPlayed) / 60000.0 
            })
            .OrderBy(x => x.Month)
            .ToListAsync();
        
        // Calculate total days for each month across all years in the date range
        var monthDayCounts = new Dictionary<int, int>();
        var currentDate = actualStartDate.Date;
        while (currentDate <= actualEndDate.Date)
        {
            var month = currentDate.Month;
            monthDayCounts[month] = monthDayCounts.GetValueOrDefault(month, 0) + 1;
            currentDate = currentDate.AddDays(1);
        }

        // Day of week stats
        var dayOfWeekStats = await query
            .GroupBy(sh => sh.PlayedAt.DayOfWeek)
            .Select(g => new { DayOfWeek = g.Key, Count = g.Count(), Minutes = g.Sum(x => x.MsPlayed) / 60000.0 })
            .ToListAsync();

        var dayNames = new[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

        // Calculate number of occurrences of each day of week in the date range
        var dayOfWeekCounts = new Dictionary<DayOfWeek, int>();
        currentDate = actualStartDate.Date; // Reset currentDate for day of week calculation
        while (currentDate <= actualEndDate.Date)
        {
            var dayOfWeek = currentDate.DayOfWeek;
            dayOfWeekCounts[dayOfWeek] = dayOfWeekCounts.GetValueOrDefault(dayOfWeek, 0) + 1;
            currentDate = currentDate.AddDays(1);
        }

        // Create time-based stats
        var timeBasedStats = new TimeBasedStats
        {
            DayOfWeekStats = dayOfWeekStats.Select(x => new DayOfWeekStats
            {
                DayOfWeek = x.DayOfWeek,
                DayName = dayNames[(int)x.DayOfWeek],
                PlayCount = x.Count,
                TotalMinutes = x.Minutes,
                AverageMinutesPerOccurrence = x.Count > 0 ? x.Minutes / x.Count : 0,
                AverageMinutesPerDay = dayOfWeekCounts.GetValueOrDefault(x.DayOfWeek, 0) > 0 
                    ? x.Minutes / dayOfWeekCounts[x.DayOfWeek] 
                    : 0
            }).ToList(),
            HourOfDayStats = hourlyStats.Select(x => new HourOfDayStats
            {
                Hour = x.Hour,
                HourLabel = $"{x.Hour:00}:00",
                TotalMinutes = x.Minutes,
                PlayCount = x.Count,
                AverageMinutesPerOccurrence = x.Count > 0 ? x.Minutes / x.Count : 0,
                AverageMinutesPerDay = daysInRange > 0 ? x.Minutes / daysInRange : 0
            }).ToList(),
            MonthlyStats = monthlyStatsByMonth.Select(x => {
                var monthName = new DateTime(2000, x.Month, 1).ToString("MMMM");
                var totalDays = monthDayCounts.GetValueOrDefault(x.Month, 0);
                return new MonthlyStats
                {
                    MonthYearLabel = monthName,
                    TotalMinutes = x.Minutes,
                    PlayCount = x.Count,
                    AverageMinutesPerDay = totalDays > 0 ? x.Minutes / totalDays : 0,
                    Month = x.Month,
                    Year = 0, // Not used when aggregating by month only
                    MonthName = monthName
                };
            }).ToList()
        };

        // Playback behavior stats
        var playbackBehavior = new PlaybackBehavior
        {
            ShufflePlays = await query.Where(x => x.Shuffle == true).CountAsync(),
            SkippedPlays = await query.Where(x => x.Skipped == true).CountAsync(),
            OfflinePlays = await query.Where(x => x.Offline == true).CountAsync(),
            IncognitoPlays = 0 // Not available in current database schema
        };

        // Start/End reasons
        var startReasons = await query
            .Where(x => x.ReasonStart != null)
            .GroupBy(x => x.ReasonStart!)
            .Select(g => new { Reason = g.Key, Count = g.Count() })
            .ToListAsync();
        
        foreach (var reason in startReasons)
        {
            playbackBehavior.StartReasons[reason.Reason] = reason.Count;
        }

        var endReasons = await query
            .Where(x => x.ReasonEnd != null)
            .GroupBy(x => x.ReasonEnd!)
            .Select(g => new { Reason = g.Key, Count = g.Count() })
            .ToListAsync();
        
        foreach (var reason in endReasons)
        {
            playbackBehavior.EndReasons[reason.Reason] = reason.Count;
        }

        // Music-only stats
        var musicQuery = query.Where(sh => sh.ContentType == "audio");
        var musicStats = new MusicStats();
        
        if (await musicQuery.AnyAsync())
        {
            var musicBasicStats = await musicQuery
                .GroupBy(x => 1)
                .Select(g => new {
                    TotalTracks = g.Count(),
                    TotalMinutes = g.Sum(x => x.MsPlayed) / 60000.0
                })
                .FirstAsync();

            musicStats.TotalMusicTracks = musicBasicStats.TotalTracks;
            musicStats.TotalMusicMinutes = musicBasicStats.TotalMinutes;
            musicStats.UniqueMusicArtists = await musicQuery
                .Where(x => x.ArtistName != null)
                .Select(x => x.ArtistName)
                .Distinct()
                .CountAsync();
            musicStats.UniqueMusicTracks = await musicQuery
                .Where(x => x.TrackName != null && x.ArtistName != null)
                .Select(x => new { x.ArtistName, x.TrackName })
                .Distinct()
                .CountAsync();
            musicStats.UniqueMusicAlbums = await musicQuery
                .Where(x => x.AlbumName != null)
                .Select(x => x.AlbumName)
                .Distinct()
                .CountAsync();
            musicStats.AverageMusicMinutesPerDay = daysInRange > 0 ? musicBasicStats.TotalMinutes / daysInRange : 0;

            // Top music artists
            musicStats.TopMusicArtists = await musicQuery
                .Where(sh => sh.ArtistName != null)
                .GroupBy(sh => sh.ArtistName)
                .Select(g => new ArtistStats
                {
                    ArtistName = g.Key!,
                    PlayCount = g.Count(),
                    TotalMinutes = g.Sum(x => x.MsPlayed) / 60000.0,
                    UniqueTracks = g.Select(x => x.TrackName).Distinct().Count(),
                    UniqueAlbums = g.Where(x => x.AlbumName != null).Select(x => x.AlbumName).Distinct().Count(),
                    PrimaryContentType = ContentType.AudioTrack
                })
                .OrderByDescending(a => a.TotalMinutes)
                .Take(50)
                .ToListAsync();

            // Top music tracks
            musicStats.TopMusicTracks = await musicQuery
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
                    ContentType = ContentType.AudioTrack,
                    AveragePlayDuration = g.Average(x => x.MsPlayed) / 60000.0,
                    MostCommonCompletionStatus = PlaybackCompletionStatus.Unknown
                })
                .OrderByDescending(t => t.TotalMinutes)
                .Take(50)
                .ToListAsync();

            // Top music albums
            musicStats.TopMusicAlbums = await musicQuery
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
                .Take(50)
                .ToListAsync();

            // Music playback behavior - skipped vs not skipped
            var totalMusicPlays = await musicQuery.CountAsync();
            var skippedPlays = await musicQuery.Where(x => x.Skipped == true).CountAsync();
            var notSkippedPlays = totalMusicPlays - skippedPlays;

            var musicPlaybackBehavior = new PlaybackBehavior
            {
                ShufflePlays = await musicQuery.Where(x => x.Shuffle == true).CountAsync(),
                SkippedPlays = skippedPlays,
                OfflinePlays = await musicQuery.Where(x => x.Offline == true).CountAsync(),
                CompletedPlays = notSkippedPlays, // Using this field to represent "not skipped"
                PartiallyCompletedPlays = 0,
                BarelyPlayedPlays = 0
            };

            musicStats.MusicPlaybackBehavior = musicPlaybackBehavior;
        }

        // Top podcasts
        var topPodcasts = await query
            .Where(sh => sh.ContentType == "podcast" && sh.ShowName != null && sh.EpisodeName != null)
            .GroupBy(sh => new { sh.ShowName, sh.EpisodeName })
            .Select(g => new PodcastStats
            {
                ShowName = g.Key.ShowName!,
                EpisodeName = g.Key.EpisodeName!,
                SpotifyUri = g.Select(x => x.SpotifyUri).FirstOrDefault(),
                PlayCount = g.Count(),
                TotalMinutes = g.Sum(x => x.MsPlayed) / 60000.0
            })
            .OrderByDescending(p => p.TotalMinutes)
            .Take(10)
            .ToListAsync();

        // Country stats
        var countryStats = await query
            .Where(sh => sh.PlayedInCountryCode != null)
            .GroupBy(sh => sh.PlayedInCountryCode!)
            .Select(g => new CountryStats
            {
                CountryCode = g.Key,
                CountryName = g.Key, // Could be enhanced with country name lookup
                PlayCount = g.Count(),
                TotalMinutes = g.Sum(x => x.MsPlayed) / 60000.0,
                UniqueTracks = g.Where(x => x.TrackName != null).Select(x => x.TrackName).Distinct().Count(),
                UniqueArtists = g.Where(x => x.ArtistName != null).Select(x => x.ArtistName).Distinct().Count()
            })
            .OrderByDescending(c => c.TotalMinutes)
            .ToListAsync();

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
            TopPodcasts = topPodcasts,
            TimeBasedStats = timeBasedStats,
            PlaybackBehavior = playbackBehavior,
            MusicStats = musicStats,
            CountryStats = countryStats
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
