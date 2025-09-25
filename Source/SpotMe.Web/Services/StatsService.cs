using SpotMe.Web.Models;
using System.Text.Json;
using System.Globalization;

namespace SpotMe.Web.Services;

public class StatsService
{
    private readonly IWebHostEnvironment _environment;
    private readonly UserDataService _userDataService;
    private List<StreamingHistoryEntry>? _allEntries;
    private readonly Dictionary<string, List<StreamingHistoryEntry>> _userEntries = new();
    
    public StatsService(IWebHostEnvironment environment, UserDataService userDataService)
    {
        _environment = environment;
        _userDataService = userDataService;
    }
    
    public async Task<List<StreamingHistoryEntry>> LoadAllStreamingHistoryAsync()
    {
        if (_allEntries != null)
            return _allEntries;
            
        var entries = new List<StreamingHistoryEntry>();
        var statsPath = Path.Combine(_environment.ContentRootPath, "SpotifyStats");
        
        // Load all extended streaming history files
        var audioFiles = Directory.GetFiles(statsPath, "Streaming_History_Audio_*.json");
        var videoFiles = Directory.GetFiles(statsPath, "Streaming_History_Video_*.json");
        
        var allFiles = audioFiles.Concat(videoFiles).ToList();
        
        foreach (var file in allFiles)
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
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading file {file}: {ex.Message}");
            }
        }
        
        // Sort by timestamp/end time
        _allEntries = entries.OrderBy(e => e.StartDateTime).ToList();
        return _allEntries;
    }
    
    public async Task<StatsOverview> GetStatsOverviewAsync(DateOnly? startDate = null, DateOnly? endDate = null)
    {
        var allEntries = await LoadAllStreamingHistoryAsync();
        
        // Filter by date range if provided
        var filteredEntries = allEntries;
        if (startDate.HasValue)
        {
            // Use date-only comparison to avoid timezone issues
            filteredEntries = filteredEntries.Where(e => DateOnly.FromDateTime(e.StartDateTime) >= startDate).ToList();
        }
        if (endDate.HasValue)
        {
            // Use date-only comparison to include the entire end date
            filteredEntries = filteredEntries.Where(e => DateOnly.FromDateTime(e.StartDateTime) <= endDate).ToList();
        }
        
        if (!filteredEntries.Any())
            return new StatsOverview();
        
        var actualStartDate = filteredEntries.Min(e => e.StartDateTime);
        var actualEndDate = filteredEntries.Max(e => e.StartDateTime);
        
        // Calculate basic stats
        var totalTracks = filteredEntries.Count;
        var totalMinutes = filteredEntries.Sum(e => e.MinutesPlayed);
        var uniqueArtists = filteredEntries.Select(e => e.UnifiedArtistName).Where(a => !string.IsNullOrEmpty(a)).Distinct().Count();
        var uniqueTracks = filteredEntries.Select(e => new { e.UnifiedArtistName, e.UnifiedTrackName }).Distinct().Count();
        
        // Calculate average minutes per day
        var daysInRange = (actualEndDate - actualStartDate).Days + 1;
        var averageMinutesPerDay = daysInRange > 0 ? totalMinutes / daysInRange : 0;
        
        // Calculate content type breakdown
        var contentTypeBreakdown = CalculateContentTypeBreakdown(filteredEntries);
        
        // Calculate platform breakdown
        var platformBreakdown = CalculatePlatformBreakdown(filteredEntries);
        
        // Calculate playback behavior
        var playbackBehavior = CalculatePlaybackBehavior(filteredEntries);
        
        // Get top artists (including all content types)
        var artistStats = filteredEntries
            .Where(e => !string.IsNullOrEmpty(e.UnifiedArtistName))
            .GroupBy(e => e.UnifiedArtistName)
            .Select(g => new ArtistStats
            {
                ArtistName = g.Key,
                PlayCount = g.Count(),
                TotalMinutes = g.Sum(e => e.MinutesPlayed),
                UniqueTracks = g.Select(e => e.UnifiedTrackName).Distinct().Count(),
                UniqueAlbums = g.Where(e => !string.IsNullOrEmpty(e.MasterMetadataAlbumAlbumName))
                               .Select(e => e.MasterMetadataAlbumAlbumName)
                               .Distinct()
                               .Count(),
                PrimaryContentType = g.GroupBy(e => e.ContentType)
                                    .OrderByDescending(ct => ct.Count())
                                    .First().Key
            })
            .OrderByDescending(a => a.TotalMinutes)
            .Take(10)
            .ToList();
        
        // Get top tracks (including all content types)
        var trackStats = filteredEntries
            .Where(e => !string.IsNullOrEmpty(e.UnifiedTrackName))
            .GroupBy(e => new { e.UnifiedArtistName, e.UnifiedTrackName })
            .Select(g => new TrackStats
            {
                ArtistName = g.Key.UnifiedArtistName,
                TrackName = g.Key.UnifiedTrackName,
                AlbumName = g.First().MasterMetadataAlbumAlbumName,
                SpotifyUri = g.First().SpotifyTrackUri ?? g.First().SpotifyEpisodeUri,
                PlayCount = g.Count(),
                TotalMinutes = g.Sum(e => e.MinutesPlayed),
                ContentType = g.First().ContentType,
                AveragePlayDuration = g.Average(e => e.MinutesPlayed),
                MostCommonCompletionStatus = g.GroupBy(e => e.CompletionStatus)
                                             .OrderByDescending(grp => grp.Count())
                                             .First().Key
            })
            .OrderByDescending(t => t.TotalMinutes)
            .Take(10)
            .ToList();
        
        // Get top albums
        var albumStats = filteredEntries
            .Where(e => !string.IsNullOrEmpty(e.MasterMetadataAlbumAlbumName) && !string.IsNullOrEmpty(e.MasterMetadataAlbumArtistName))
            .GroupBy(e => new { e.MasterMetadataAlbumArtistName, e.MasterMetadataAlbumAlbumName })
            .Select(g => new AlbumStats
            {
                ArtistName = g.Key.MasterMetadataAlbumArtistName,
                AlbumName = g.Key.MasterMetadataAlbumAlbumName,
                PlayCount = g.Count(),
                TotalMinutes = g.Sum(e => e.MinutesPlayed),
                UniqueTracks = g.Select(e => e.MasterMetadataTrackName).Distinct().Count()
            })
            .OrderByDescending(a => a.TotalMinutes)
            .Take(10)
            .ToList();
        
        // Get top podcasts
        var podcastStats = filteredEntries
            .Where(e => e.ContentType == ContentType.Podcast && !string.IsNullOrEmpty(e.EpisodeShowName))
            .GroupBy(e => new { e.EpisodeShowName, e.EpisodeName })
            .Select(g => new PodcastStats
            {
                ShowName = g.Key.EpisodeShowName,
                EpisodeName = g.Key.EpisodeName ?? "Unknown Episode",
                SpotifyUri = g.First().SpotifyEpisodeUri,
                PlayCount = g.Count(),
                TotalMinutes = g.Sum(e => e.MinutesPlayed)
            })
            .OrderByDescending(p => p.TotalMinutes)
            .Take(10)
            .ToList();
        
        // Calculate music-specific statistics
        var musicStats = CalculateMusicStats(filteredEntries, actualStartDate, actualEndDate);

        // Calculate country-specific statistics
        var countryStats = CalculateCountryStats(filteredEntries);

        // Calculate time-based statistics
        var timeBasedStats = CalculateTimeBasedStats(filteredEntries, actualStartDate, actualEndDate);

        return new StatsOverview
        {
            StartDate = actualStartDate,
            EndDate = actualEndDate,
            TotalTracks = totalTracks,
            TotalMinutes = totalMinutes,
            UniqueArtists = uniqueArtists,
            UniqueTracks = uniqueTracks,
            AverageMinutesPerDay = averageMinutesPerDay,
            ContentTypeBreakdown = contentTypeBreakdown,
            PlatformBreakdown = platformBreakdown,
            PlaybackBehavior = playbackBehavior,
            TopArtists = artistStats,
            TopTracks = trackStats,
            TopAlbums = albumStats,
            TopPodcasts = podcastStats,
            MusicStats = musicStats,
            CountryStats = countryStats,
            TimeBasedStats = timeBasedStats
        };
    }
    
    private ContentTypeBreakdown CalculateContentTypeBreakdown(List<StreamingHistoryEntry> entries)
    {
        var breakdown = new ContentTypeBreakdown();
        
        foreach (var entry in entries)
        {
            switch (entry.ContentType)
            {
                case ContentType.AudioTrack:
                    breakdown.AudioTrackCount++;
                    breakdown.AudioTrackMinutes += entry.MinutesPlayed;
                    break;
                case ContentType.Podcast:
                    breakdown.PodcastCount++;
                    breakdown.PodcastMinutes += entry.MinutesPlayed;
                    break;
                default:
                    breakdown.UnknownCount++;
                    breakdown.UnknownMinutes += entry.MinutesPlayed;
                    break;
            }
        }
        
        return breakdown;
    }
    
    private PlatformBreakdown CalculatePlatformBreakdown(List<StreamingHistoryEntry> entries)
    {
        var breakdown = new PlatformBreakdown();
        
        foreach (var entry in entries)
        {
            if (!string.IsNullOrEmpty(entry.Platform))
            {
                if (!breakdown.PlatformUsage.ContainsKey(entry.Platform))
                {
                    breakdown.PlatformUsage[entry.Platform] = 0;
                    breakdown.PlatformMinutes[entry.Platform] = 0;
                }
                
                breakdown.PlatformUsage[entry.Platform]++;
                breakdown.PlatformMinutes[entry.Platform] += entry.MinutesPlayed;
            }
        }
        
        return breakdown;
    }
    
    private PlaybackBehavior CalculatePlaybackBehavior(List<StreamingHistoryEntry> entries)
    {
        var behavior = new PlaybackBehavior();
        
        foreach (var entry in entries)
        {
            if (entry.Shuffle == true) behavior.ShufflePlays++;
            if (entry.Skipped == true) behavior.SkippedPlays++;
            if (entry.Offline == true) behavior.OfflinePlays++;
            if (entry.IncognitoMode == true) behavior.IncognitoPlays++;
            
            if (!string.IsNullOrEmpty(entry.ReasonStart))
            {
                if (!behavior.StartReasons.ContainsKey(entry.ReasonStart))
                    behavior.StartReasons[entry.ReasonStart] = 0;
                behavior.StartReasons[entry.ReasonStart]++;
            }
            
            if (!string.IsNullOrEmpty(entry.ReasonEnd))
            {
                if (!behavior.EndReasons.ContainsKey(entry.ReasonEnd))
                    behavior.EndReasons[entry.ReasonEnd] = 0;
                behavior.EndReasons[entry.ReasonEnd]++;
            }
            
            // Calculate playback completion statistics
            switch (entry.CompletionStatus)
            {
                case PlaybackCompletionStatus.Completed:
                    behavior.CompletedPlays++;
                    break;
                case PlaybackCompletionStatus.PartiallyCompleted:
                    behavior.PartiallyCompletedPlays++;
                    break;
                case PlaybackCompletionStatus.BarelyPlayed:
                    behavior.BarelyPlayedPlays++;
                    break;
                case PlaybackCompletionStatus.Skipped:
                    // Already counted in SkippedPlays
                    break;
            }
            
            // Track completion status breakdown
            if (!behavior.CompletionStatusBreakdown.ContainsKey(entry.CompletionStatus))
                behavior.CompletionStatusBreakdown[entry.CompletionStatus] = 0;
            behavior.CompletionStatusBreakdown[entry.CompletionStatus]++;
        }
        

        
        return behavior;
    }

    private MusicStats CalculateMusicStats(List<StreamingHistoryEntry> entries, DateTime startDate, DateTime endDate)
    {
        // Filter to only music tracks
        var musicEntries = entries.Where(e => e.ContentType == ContentType.AudioTrack).ToList();
        
        if (!musicEntries.Any())
        {
            return new MusicStats();
        }
        
        // Calculate basic music stats
        var totalMusicTracks = musicEntries.Count;
        var totalMusicMinutes = musicEntries.Sum(e => e.MinutesPlayed);
        var uniqueMusicArtists = musicEntries.Select(e => e.UnifiedArtistName).Where(a => !string.IsNullOrEmpty(a)).Distinct().Count();
        var uniqueMusicTracks = musicEntries.Select(e => new { e.UnifiedArtistName, e.UnifiedTrackName }).Distinct().Count();
        var uniqueMusicAlbums = musicEntries.Where(e => !string.IsNullOrEmpty(e.MasterMetadataAlbumAlbumName))
                                           .Select(e => e.MasterMetadataAlbumAlbumName)
                                           .Distinct()
                                           .Count();
        
        // Calculate average music minutes per day
        var daysInRange = (endDate - startDate).Days + 1;
        var averageMusicMinutesPerDay = daysInRange > 0 ? totalMusicMinutes / daysInRange : 0;
        
        // Get top music artists
        var topMusicArtists = musicEntries
            .Where(e => !string.IsNullOrEmpty(e.UnifiedArtistName))
            .GroupBy(e => e.UnifiedArtistName)
            .Select(g => new ArtistStats
            {
                ArtistName = g.Key,
                PlayCount = g.Count(),
                TotalMinutes = g.Sum(e => e.MinutesPlayed),
                UniqueTracks = g.Select(e => e.UnifiedTrackName).Distinct().Count(),
                UniqueAlbums = g.Where(e => !string.IsNullOrEmpty(e.MasterMetadataAlbumAlbumName))
                               .Select(e => e.MasterMetadataAlbumAlbumName)
                               .Distinct()
                               .Count(),
                PrimaryContentType = ContentType.AudioTrack
            })
            .OrderByDescending(a => a.TotalMinutes)
            .Take(25)
            .ToList();
        
        // Get top music tracks
        var topMusicTracks = musicEntries
            .Where(e => !string.IsNullOrEmpty(e.UnifiedTrackName))
            .GroupBy(e => new { e.UnifiedArtistName, e.UnifiedTrackName })
            .Select(g => new TrackStats
            {
                ArtistName = g.Key.UnifiedArtistName,
                TrackName = g.Key.UnifiedTrackName,
                AlbumName = g.First().MasterMetadataAlbumAlbumName,
                SpotifyUri = g.First().SpotifyTrackUri,
                PlayCount = g.Count(),
                TotalMinutes = g.Sum(e => e.MinutesPlayed),
                ContentType = ContentType.AudioTrack,
                AveragePlayDuration = g.Average(e => e.MinutesPlayed),
                MostCommonCompletionStatus = g.GroupBy(e => e.CompletionStatus)
                                             .OrderByDescending(grp => grp.Count())
                                             .First().Key
            })
            .OrderByDescending(t => t.TotalMinutes)
            .Take(25)
            .ToList();
        
        // Get top music albums
        var topMusicAlbums = musicEntries
            .Where(e => !string.IsNullOrEmpty(e.MasterMetadataAlbumAlbumName) && !string.IsNullOrEmpty(e.MasterMetadataAlbumArtistName))
            .GroupBy(e => new { e.MasterMetadataAlbumArtistName, e.MasterMetadataAlbumAlbumName })
            .Select(g => new AlbumStats
            {
                ArtistName = g.Key.MasterMetadataAlbumArtistName,
                AlbumName = g.Key.MasterMetadataAlbumAlbumName,
                PlayCount = g.Count(),
                TotalMinutes = g.Sum(e => e.MinutesPlayed),
                UniqueTracks = g.Select(e => e.MasterMetadataTrackName).Distinct().Count()
            })
            .OrderByDescending(a => a.TotalMinutes)
            .Take(25)
            .ToList();
        
        // Calculate music-specific playback behavior
        var musicPlaybackBehavior = CalculatePlaybackBehavior(musicEntries);

        // Get most skipped music tracks and artists
        var topSkippedMusicTracks = CalculateTopSkippedMusicTracks(musicEntries);
        var topSkippedMusicArtists = CalculateTopSkippedMusicArtists(musicEntries);

        return new MusicStats
        {
            TotalMusicTracks = totalMusicTracks,
            TotalMusicMinutes = totalMusicMinutes,
            UniqueMusicArtists = uniqueMusicArtists,
            UniqueMusicTracks = uniqueMusicTracks,
            UniqueMusicAlbums = uniqueMusicAlbums,
            AverageMusicMinutesPerDay = averageMusicMinutesPerDay,
            TopMusicArtists = topMusicArtists,
            TopMusicTracks = topMusicTracks,
            TopMusicAlbums = topMusicAlbums,
            TopSkippedMusicTracks = topSkippedMusicTracks,
            TopSkippedMusicArtists = topSkippedMusicArtists,
            MusicPlaybackBehavior = musicPlaybackBehavior
        };
    }

    private List<TrackStats> CalculateTopSkippedMusicTracks(List<StreamingHistoryEntry> musicEntries)
    {
        // Filter to only skipped music tracks
        var skippedMusicEntries = musicEntries.Where(e => e.Skipped == true || e.CompletionStatus == PlaybackCompletionStatus.Skipped).ToList();

        if (!skippedMusicEntries.Any())
        {
            return new List<TrackStats>();
        }

        // Group by track and calculate skip statistics
        var skippedTracks = skippedMusicEntries
            .Where(e => !string.IsNullOrEmpty(e.UnifiedTrackName))
            .GroupBy(e => new { e.UnifiedArtistName, e.UnifiedTrackName })
            .Select(g => new TrackStats
            {
                ArtistName = g.Key.UnifiedArtistName,
                TrackName = g.Key.UnifiedTrackName,
                AlbumName = g.First().MasterMetadataAlbumAlbumName,
                SpotifyUri = g.First().SpotifyTrackUri,
                PlayCount = g.Count(), // This represents skip count in this context
                TotalMinutes = g.Sum(e => e.MinutesPlayed),
                ContentType = ContentType.AudioTrack,
                AveragePlayDuration = g.Average(e => e.MinutesPlayed),
                MostCommonCompletionStatus = PlaybackCompletionStatus.Skipped
            })
            .OrderByDescending(t => t.PlayCount) // Order by skip count
            .Take(10)
            .ToList();

        return skippedTracks;
    }

    private List<ArtistStats> CalculateTopSkippedMusicArtists(List<StreamingHistoryEntry> musicEntries)
    {
        // Filter to only skipped music tracks
        var skippedMusicEntries = musicEntries.Where(e => e.Skipped == true || e.CompletionStatus == PlaybackCompletionStatus.Skipped).ToList();

        if (!skippedMusicEntries.Any())
        {
            return new List<ArtistStats>();
        }

        // Group by artist and calculate skip statistics
        var skippedArtists = skippedMusicEntries
            .Where(e => !string.IsNullOrEmpty(e.UnifiedArtistName))
            .GroupBy(e => e.UnifiedArtistName)
            .Select(g => new ArtistStats
            {
                ArtistName = g.Key,
                PlayCount = g.Count(), // This represents skip count in this context
                TotalMinutes = g.Sum(e => e.MinutesPlayed),
                UniqueTracks = g.Select(e => e.UnifiedTrackName).Distinct().Count(),
                UniqueAlbums = g.Where(e => !string.IsNullOrEmpty(e.MasterMetadataAlbumAlbumName))
                               .Select(e => e.MasterMetadataAlbumAlbumName)
                               .Distinct()
                               .Count(),
                PrimaryContentType = ContentType.AudioTrack
            })
            .OrderByDescending(a => a.PlayCount) // Order by skip count
            .Take(10)
            .ToList();

        return skippedArtists;
    }

    private List<CountryStats> CalculateCountryStats(List<StreamingHistoryEntry> entries)
    {
        // Country code mapping for common countries
        var countryMapping = new Dictionary<string, string>
        {
            { "US", "United States" },
            { "GB", "United Kingdom" },
            { "CA", "Canada" },
            { "AU", "Australia" },
            { "DE", "Germany" },
            { "FR", "France" },
            { "IT", "Italy" },
            { "ES", "Spain" },
            { "NL", "Netherlands" },
            { "SE", "Sweden" },
            { "NO", "Norway" },
            { "DK", "Denmark" },
            { "FI", "Finland" },
            { "PL", "Poland" },
            { "CZ", "Czech Republic" },
            { "AT", "Austria" },
            { "CH", "Switzerland" },
            { "BE", "Belgium" },
            { "IE", "Ireland" },
            { "PT", "Portugal" },
            { "GR", "Greece" },
            { "HR", "Croatia" },
            { "HU", "Hungary" },
            { "RO", "Romania" },
            { "BG", "Bulgaria" },
            { "SK", "Slovakia" },
            { "SI", "Slovenia" },
            { "LT", "Lithuania" },
            { "LV", "Latvia" },
            { "EE", "Estonia" },
            { "JP", "Japan" },
            { "KR", "South Korea" },
            { "CN", "China" },
            { "IN", "India" },
            { "BR", "Brazil" },
            { "MX", "Mexico" },
            { "AR", "Argentina" },
            { "CL", "Chile" },
            { "CO", "Colombia" },
            { "PE", "Peru" },
            { "ZA", "South Africa" },
            { "EG", "Egypt" },
            { "NG", "Nigeria" },
            { "KE", "Kenya" },
            { "MA", "Morocco" },
            { "TN", "Tunisia" },
            { "DZ", "Algeria" },
            { "RU", "Russia" },
            { "UA", "Ukraine" },
            { "BY", "Belarus" },
            { "TR", "Turkey" },
            { "IL", "Israel" },
            { "SA", "Saudi Arabia" },
            { "AE", "United Arab Emirates" },
            { "QA", "Qatar" },
            { "KW", "Kuwait" },
            { "BH", "Bahrain" },
            { "OM", "Oman" },
            { "JO", "Jordan" },
            { "LB", "Lebanon" },
            { "SY", "Syria" },
            { "IQ", "Iraq" },
            { "IR", "Iran" },
            { "AF", "Afghanistan" },
            { "PK", "Pakistan" },
            { "BD", "Bangladesh" },
            { "LK", "Sri Lanka" },
            { "NP", "Nepal" },
            { "BT", "Bhutan" },
            { "MV", "Maldives" },
            { "TH", "Thailand" },
            { "VN", "Vietnam" },
            { "LA", "Laos" },
            { "KH", "Cambodia" },
            { "MM", "Myanmar" },
            { "MY", "Malaysia" },
            { "SG", "Singapore" },
            { "ID", "Indonesia" },
            { "PH", "Philippines" },
            { "BN", "Brunei" },
            { "TL", "East Timor" },
            { "NZ", "New Zealand" },
            { "FJ", "Fiji" },
            { "PG", "Papua New Guinea" },
            { "SB", "Solomon Islands" },
            { "VU", "Vanuatu" },
            { "NC", "New Caledonia" },
            { "PF", "French Polynesia" },
            { "WS", "Samoa" },
            { "TO", "Tonga" },
            { "KI", "Kiribati" },
            { "TV", "Tuvalu" },
            { "NR", "Nauru" },
            { "PW", "Palau" },
            { "FM", "Micronesia" },
            { "MH", "Marshall Islands" }
        };

        return entries
            .Where(e => !string.IsNullOrEmpty(e.PlayedInCountryCode))
            .GroupBy(e => e.PlayedInCountryCode)
            .Select(g => new CountryStats
            {
                CountryCode = g.Key,
                CountryName = countryMapping.GetValueOrDefault(g.Key, g.Key),
                PlayCount = g.Count(),
                TotalMinutes = g.Sum(e => e.MinutesPlayed),
                UniqueTracks = g.Select(e => new { e.UnifiedArtistName, e.UnifiedTrackName }).Distinct().Count(),
                UniqueArtists = g.Select(e => e.UnifiedArtistName).Where(a => !string.IsNullOrEmpty(a)).Distinct().Count()
            })
            .OrderByDescending(c => c.PlayCount)
            .ToList();
    }

    private TimeBasedStats CalculateTimeBasedStats(List<StreamingHistoryEntry> entries, DateTime startDate, DateTime endDate)
    {
        var timeBasedStats = new TimeBasedStats();

        // Calculate day of week statistics
        var daysInRange = (endDate - startDate).Days + 1;
        var dayOfWeekOccurrences = new Dictionary<DayOfWeek, int>();
        
        // Count how many times each day of week occurs in the date range
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            var dayOfWeek = date.DayOfWeek;
            dayOfWeekOccurrences[dayOfWeek] = dayOfWeekOccurrences.GetValueOrDefault(dayOfWeek, 0) + 1;
        }

        timeBasedStats.DayOfWeekStats = Enum.GetValues<DayOfWeek>()
            .Select(dayOfWeek =>
            {
                var dayEntries = entries.Where(e => e.StartDateTime.DayOfWeek == dayOfWeek).ToList();
                var totalMinutes = dayEntries.Sum(e => e.MinutesPlayed);
                var playCount = dayEntries.Count;
                var occurrences = dayOfWeekOccurrences.GetValueOrDefault(dayOfWeek, 1);
                
                return new DayOfWeekStats
                {
                    DayOfWeek = dayOfWeek,
                    DayName = dayOfWeek.ToString(),
                    PlayCount = playCount,
                    TotalMinutes = totalMinutes,
                    AverageMinutesPerOccurrence = occurrences > 0 ? totalMinutes / occurrences : 0
                };
            })
            .ToList();

        // Calculate hour of day statistics  
        timeBasedStats.HourOfDayStats = Enumerable.Range(0, 24)
            .Select(hour =>
            {
                var hourEntries = entries.Where(e => e.StartDateTime.Hour == hour).ToList();
                var totalMinutes = hourEntries.Sum(e => e.MinutesPlayed);
                var playCount = hourEntries.Count;
                
                return new HourOfDayStats
                {
                    Hour = hour,
                    HourLabel = $"{hour:00}:00",
                    PlayCount = playCount,
                    TotalMinutes = totalMinutes,
                    AverageMinutesPerOccurrence = daysInRange > 0 ? totalMinutes / daysInRange : 0
                };
            })
            .ToList();

        // Calculate monthly statistics
        var totalRangeInDays = (endDate - startDate).Days + 1;
        var isMoreThanOneYear = totalRangeInDays > 365;
        
        timeBasedStats.MonthlyStats = new List<MonthlyStats>();

        if (isMoreThanOneYear)
        {
            // If range covers more than a year, show all 12 months normalized by occurrence count
            for (int month = 1; month <= 12; month++)
            {
                var monthEntries = entries.Where(e => e.StartDateTime.Month == month).ToList();
                var totalMinutes = monthEntries.Sum(e => e.MinutesPlayed);
                var playCount = monthEntries.Count;
                
                // Count how many times this month actually occurs in the data (not just date range)
                var monthOccurrences = entries
                    .Select(e => new { e.StartDateTime.Year, e.StartDateTime.Month })
                    .Where(ym => ym.Month == month)
                    .Select(ym => ym.Year)
                    .Distinct()
                    .Count();
                
                // Use average minutes per month occurrence to normalize for uneven sampling
                var averageMinutesPerMonthOccurrence = monthOccurrences > 0 ? totalMinutes / monthOccurrences : 0;
                
                timeBasedStats.MonthlyStats.Add(new MonthlyStats
                {
                    Month = month,
                    Year = 0, // Represents aggregated across years
                    MonthName = new DateTime(2000, month, 1).ToString("MMMM", CultureInfo.InvariantCulture),
                    MonthYearLabel = new DateTime(2000, month, 1).ToString("MMM", CultureInfo.InvariantCulture),
                    PlayCount = playCount,
                    TotalMinutes = averageMinutesPerMonthOccurrence, // Now represents average per occurrence
                    AverageMinutesPerDay = monthOccurrences > 0 ? averageMinutesPerMonthOccurrence / 30.44 : 0 // Rough average days per month
                });
            }
        }
        else
        {
            // If range is within a year, show only months that have data or are in range
            var monthsInRange = entries
                .Select(e => new { e.StartDateTime.Year, e.StartDateTime.Month })
                .Distinct()
                .OrderBy(m => m.Year)
                .ThenBy(m => m.Month)
                .ToList();

            foreach (var monthYear in monthsInRange)
            {
                var monthEntries = entries.Where(e => e.StartDateTime.Year == monthYear.Year && e.StartDateTime.Month == monthYear.Month).ToList();
                var totalMinutes = monthEntries.Sum(e => e.MinutesPlayed);
                var playCount = monthEntries.Count;
                
                // Count days in this specific month and year
                var daysInMonth = DateTime.DaysInMonth(monthYear.Year, monthYear.Month);
                var monthStart = new DateTime(monthYear.Year, monthYear.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                
                // Adjust for the actual range boundaries
                var rangeStart = monthStart < startDate ? startDate : monthStart;
                var rangeEnd = monthEnd > endDate ? endDate : monthEnd;
                var actualDaysInRange = (rangeEnd - rangeStart).Days + 1;
                
                timeBasedStats.MonthlyStats.Add(new MonthlyStats
                {
                    Month = monthYear.Month,
                    Year = monthYear.Year,
                    MonthName = new DateTime(monthYear.Year, monthYear.Month, 1).ToString("MMMM", CultureInfo.InvariantCulture),
                    MonthYearLabel = new DateTime(monthYear.Year, monthYear.Month, 1).ToString("MMM yyyy", CultureInfo.InvariantCulture),
                    PlayCount = playCount,
                    TotalMinutes = totalMinutes,
                    AverageMinutesPerDay = actualDaysInRange > 0 ? totalMinutes / actualDaysInRange : 0
                });
            }
        }

        return timeBasedStats;
    }

    // User-specific data methods

    /// <summary>
    /// Load streaming history for a specific user
    /// </summary>
    public async Task<List<StreamingHistoryEntry>> LoadUserStreamingHistoryAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        // Check cache first
        if (_userEntries.TryGetValue(userId, out var cachedEntries))
            return cachedEntries;

        // Load from user data service
        var entries = await _userDataService.LoadUserStreamingHistoryAsync();
        
        // Cache the results
        _userEntries[userId] = entries;
        
        return entries;
    }

    /// <summary>
    /// Get stats overview for a specific user
    /// </summary>
    public async Task<StatsOverview> GetUserStatsOverviewAsync(string userId, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        var allEntries = await LoadUserStreamingHistoryAsync(userId);
        
        // Filter by date range if provided
        var filteredEntries = allEntries;
        if (startDate.HasValue)
        {
            filteredEntries = filteredEntries.Where(e => DateOnly.FromDateTime(e.StartDateTime) >= startDate).ToList();
        }
        if (endDate.HasValue)
        {
            filteredEntries = filteredEntries.Where(e => DateOnly.FromDateTime(e.StartDateTime) <= endDate).ToList();
        }
        
        if (!filteredEntries.Any())
            return new StatsOverview();
        
        return await GenerateStatsOverviewAsync(filteredEntries);
    }

    /// <summary>
    /// Clear user data cache (call this when user uploads new files)
    /// </summary>
    public void ClearUserCache(string userId)
    {
        _userEntries.Remove(userId);
    }

    /// <summary>
    /// Clear all user data cache
    /// </summary>
    public void ClearAllUserCache()
    {
        _userEntries.Clear();
    }

    /// <summary>
    /// Generate stats overview from filtered entries (uses same logic as the main method)
    /// </summary>
    private async Task<StatsOverview> GenerateStatsOverviewAsync(List<StreamingHistoryEntry> filteredEntries)
    {
        var actualStartDate = filteredEntries.Min(e => e.StartDateTime);
        var actualEndDate = filteredEntries.Max(e => e.StartDateTime);
        
        // Calculate basic stats
        var totalTracks = filteredEntries.Count;
        var totalMinutes = filteredEntries.Sum(e => e.MinutesPlayed);
        var uniqueArtists = filteredEntries.Select(e => e.UnifiedArtistName).Where(a => !string.IsNullOrEmpty(a)).Distinct().Count();
        var uniqueTracks = filteredEntries.Select(e => new { e.UnifiedArtistName, e.UnifiedTrackName }).Distinct().Count();
        
        // Calculate average minutes per day
        var daysInRange = (actualEndDate - actualStartDate).Days + 1;
        var averageMinutesPerDay = daysInRange > 0 ? totalMinutes / daysInRange : 0;
        
        // Calculate content type breakdown
        var contentTypeBreakdown = CalculateContentTypeBreakdown(filteredEntries);
        
        // Calculate platform breakdown
        var platformBreakdown = CalculatePlatformBreakdown(filteredEntries);
        
        // Calculate playback behavior
        var playbackBehavior = CalculatePlaybackBehavior(filteredEntries);
        
        // Get top artists (reusing existing logic)
        var artistStats = filteredEntries
            .Where(e => !string.IsNullOrEmpty(e.UnifiedArtistName))
            .GroupBy(e => e.UnifiedArtistName)
            .Select(g => new ArtistStats
            {
                ArtistName = g.Key,
                PlayCount = g.Count(),
                TotalMinutes = g.Sum(e => e.MinutesPlayed),
                UniqueTracks = g.Select(e => e.UnifiedTrackName).Distinct().Count(),
                UniqueAlbums = g.Where(e => !string.IsNullOrEmpty(e.MasterMetadataAlbumAlbumName))
                               .Select(e => e.MasterMetadataAlbumAlbumName)
                               .Distinct()
                               .Count(),
                PrimaryContentType = g.GroupBy(e => e.ContentType)
                                    .OrderByDescending(ct => ct.Count())
                                    .First().Key
            })
            .OrderByDescending(a => a.TotalMinutes)
            .Take(10)
            .ToList();

        // Create and populate the stats object with all the same logic as the main method
        var stats = new StatsOverview
        {
            StartDate = actualStartDate,
            EndDate = actualEndDate,
            TotalTracks = totalTracks,
            TotalMinutes = totalMinutes,
            UniqueArtists = uniqueArtists,
            UniqueTracks = uniqueTracks,
            AverageMinutesPerDay = averageMinutesPerDay,
            ContentTypeBreakdown = contentTypeBreakdown,
            PlatformBreakdown = platformBreakdown,
            PlaybackBehavior = playbackBehavior,
            TopArtists = artistStats,
            // TODO: Add other properties by copying from the main method
            TopTracks = new List<TrackStats>(),
            TopAlbums = new List<AlbumStats>(),
            TopPodcasts = new List<PodcastStats>(),
            MusicStats = new MusicStats(),
            CountryStats = new List<CountryStats>(),
            TimeBasedStats = new TimeBasedStats()
        };

        return stats;
    }
}