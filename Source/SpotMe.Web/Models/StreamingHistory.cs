using System.Text.Json.Serialization;

namespace SpotMe.Web.Models;

public class StreamingHistoryEntry
{
    // Legacy fields (for backward compatibility)
    [JsonPropertyName("artistName")]
    public string? ArtistName { get; set; }
    
    [JsonPropertyName("trackName")]
    public string? TrackName { get; set; }
    
    [JsonPropertyName("ms_played")]
    public long MsPlayed { get; set; }

    // Extended fields
    [JsonPropertyName("ts")]
    public string? Timestamp { get; set; }
    
    [JsonPropertyName("platform")]
    public string? Platform { get; set; }
    
    [JsonPropertyName("conn_country")]
    public string? PlayedInCountryCode { get; set; }
    
    [JsonPropertyName("ip_addr")]
    public string? IpAddress { get; set; }
    
    // Master metadata for audio tracks
    [JsonPropertyName("master_metadata_track_name")]
    public string? MasterMetadataTrackName { get; set; }
    
    [JsonPropertyName("master_metadata_album_artist_name")]
    public string? MasterMetadataAlbumArtistName { get; set; }
    
    [JsonPropertyName("master_metadata_album_album_name")]
    public string? MasterMetadataAlbumAlbumName { get; set; }
    
    [JsonPropertyName("spotify_track_uri")]
    public string? SpotifyTrackUri { get; set; }
    
    // Podcast metadata
    [JsonPropertyName("episode_name")]
    public string? EpisodeName { get; set; }
    
    [JsonPropertyName("episode_show_name")]
    public string? EpisodeShowName { get; set; }
    
    [JsonPropertyName("spotify_episode_uri")]
    public string? SpotifyEpisodeUri { get; set; }
    
    // Audiobook metadata (exists in JSON but not used)
    [JsonPropertyName("audiobook_title")]
    public string? AudiobookTitle { get; set; }
    
    [JsonPropertyName("audiobook_uri")]
    public string? AudiobookUri { get; set; }
    
    [JsonPropertyName("audiobook_chapter_uri")]
    public string? AudiobookChapterUri { get; set; }
    
    [JsonPropertyName("audiobook_chapter_title")]
    public string? AudiobookChapterTitle { get; set; }
    
    // Playback context
    [JsonPropertyName("reason_start")]
    public string? ReasonStart { get; set; }
    
    [JsonPropertyName("reason_end")]
    public string? ReasonEnd { get; set; }
    
    [JsonPropertyName("shuffle")]
    public bool? Shuffle { get; set; }
    
    [JsonPropertyName("skipped")]
    public bool? Skipped { get; set; }
    
    [JsonPropertyName("offline")]
    public bool? Offline { get; set; }
    
    [JsonPropertyName("offline_timestamp")]
    public long? OfflineTimestamp { get; set; }
    
    [JsonPropertyName("incognito_mode")]
    public bool? IncognitoMode { get; set; }

    // Use Timestamp for all date logic
    public DateTime StartDateTime => !string.IsNullOrEmpty(Timestamp) ? DateTime.Parse(Timestamp, null, System.Globalization.DateTimeStyles.RoundtripKind) : DateTime.MinValue;
    
    public TimeSpan Duration => TimeSpan.FromMilliseconds(MsPlayed);
    public double MinutesPlayed => Duration.TotalMinutes;

    // Playback completion status based on reason_end and ms_played
    public PlaybackCompletionStatus CompletionStatus
    {
        get
        {
            if (Skipped == true)
                return PlaybackCompletionStatus.Skipped;
            
            // Use reason_end to determine completion
            if (ReasonEnd == "endplay" || ReasonEnd == "trackdone")
                return PlaybackCompletionStatus.Completed;
            else if (ReasonEnd == "fwdbtn" || ReasonEnd == "backbtn")
                return PlaybackCompletionStatus.PartiallyCompleted;
            else if (MsPlayed < 30000) // Less than 30 seconds
                return PlaybackCompletionStatus.BarelyPlayed;
            else
                return PlaybackCompletionStatus.PartiallyCompleted;
        }
    }

    public ContentType ContentType
    {
        get
        {
            if (!string.IsNullOrEmpty(MasterMetadataTrackName) || !string.IsNullOrEmpty(SpotifyTrackUri))
                return ContentType.AudioTrack;
            if (!string.IsNullOrEmpty(EpisodeName) || !string.IsNullOrEmpty(SpotifyEpisodeUri))
                return ContentType.Podcast;
            return ContentType.Unknown;
        }
    }

    // Unified artist name (for backward compatibility)
    public string UnifiedArtistName => 
        MasterMetadataAlbumArtistName ?? ArtistName ?? string.Empty;

    // Unified track name (for backward compatibility)
    public string UnifiedTrackName => 
        MasterMetadataTrackName ?? TrackName ?? EpisodeName ?? string.Empty;
}

public enum ContentType
{
    Unknown,
    AudioTrack,
    Podcast
}

public enum PlaybackCompletionStatus
{
    Skipped,
    BarelyPlayed,
    PartiallyCompleted,
    Completed
}

public class StatsOverview
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalTracks { get; set; }
    public double TotalMinutes { get; set; }
    public int UniqueArtists { get; set; }
    public int UniqueTracks { get; set; }
    public double AverageMinutesPerDay { get; set; }
    
    // Extended statistics
    public ContentTypeBreakdown ContentTypeBreakdown { get; set; } = new();
    public PlatformBreakdown PlatformBreakdown { get; set; } = new();
    public PlaybackBehavior PlaybackBehavior { get; set; } = new();
    public List<ArtistStats> TopArtists { get; set; } = new();
    public List<TrackStats> TopTracks { get; set; } = new();
    public List<AlbumStats> TopAlbums { get; set; } = new();
    public List<PodcastStats> TopPodcasts { get; set; } = new();
    
    // Music-specific statistics
    public MusicStats MusicStats { get; set; } = new();

        // Country-specific statistics
    public List<CountryStats> CountryStats { get; set; } = new();

    // Time-based statistics
    public TimeBasedStats TimeBasedStats { get; set; } = new();
}

public class ContentTypeBreakdown
{
    public int AudioTrackCount { get; set; }
    public double AudioTrackMinutes { get; set; }
    public int PodcastCount { get; set; }
    public double PodcastMinutes { get; set; }
    public int UnknownCount { get; set; }
    public double UnknownMinutes { get; set; }
}

public class PlatformBreakdown
{
    public Dictionary<string, int> PlatformUsage { get; set; } = new();
    public Dictionary<string, double> PlatformMinutes { get; set; } = new();
}

public class PlaybackBehavior
{
    public int ShufflePlays { get; set; }
    public int SkippedPlays { get; set; }
    public int OfflinePlays { get; set; }
    public int IncognitoPlays { get; set; }
    public Dictionary<string, int> StartReasons { get; set; } = new();
    public Dictionary<string, int> EndReasons { get; set; } = new();
    
    // Playback completion statistics
    public int CompletedPlays { get; set; }
    public int PartiallyCompletedPlays { get; set; }
    public int BarelyPlayedPlays { get; set; }
    public Dictionary<PlaybackCompletionStatus, int> CompletionStatusBreakdown { get; set; } = new();
}

public class ArtistStats
{
    public string ArtistName { get; set; } = string.Empty;
    public int PlayCount { get; set; }
    public double TotalMinutes { get; set; }
    public int UniqueTracks { get; set; }
    public int UniqueAlbums { get; set; }
    public ContentType PrimaryContentType { get; set; }
}

public class TrackStats
{
    public string ArtistName { get; set; } = string.Empty;
    public string TrackName { get; set; } = string.Empty;
    public string? AlbumName { get; set; }
    public string? SpotifyUri { get; set; }
    public int PlayCount { get; set; }
    public double TotalMinutes { get; set; }
    public ContentType ContentType { get; set; }
    public double AveragePlayDuration { get; set; }
    public PlaybackCompletionStatus MostCommonCompletionStatus { get; set; }
}

public class AlbumStats
{
    public string ArtistName { get; set; } = string.Empty;
    public string AlbumName { get; set; } = string.Empty;
    public int PlayCount { get; set; }
    public double TotalMinutes { get; set; }
    public int UniqueTracks { get; set; }
}

public class PodcastStats
{
    public string ShowName { get; set; } = string.Empty;
    public string EpisodeName { get; set; } = string.Empty;
    public string? SpotifyUri { get; set; }
    public int PlayCount { get; set; }
    public double TotalMinutes { get; set; }
}

public class MusicStats
{
    public int TotalMusicTracks { get; set; }
    public double TotalMusicMinutes { get; set; }
    public int UniqueMusicArtists { get; set; }
    public int UniqueMusicTracks { get; set; }
    public int UniqueMusicAlbums { get; set; }
    public double AverageMusicMinutesPerDay { get; set; }
    public List<ArtistStats> TopMusicArtists { get; set; } = new();
    public List<TrackStats> TopMusicTracks { get; set; } = new();
    public List<AlbumStats> TopMusicAlbums { get; set; } = new();
    public List<TrackStats> TopSkippedMusicTracks { get; set; } = new();
    public List<ArtistStats> TopSkippedMusicArtists { get; set; } = new();
    public PlaybackBehavior MusicPlaybackBehavior { get; set; } = new();
}

public class CountryStats
{
    public string CountryCode { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;
    public int PlayCount { get; set; }
    public double TotalMinutes { get; set; }
    public int UniqueTracks { get; set; }
    public int UniqueArtists { get; set; }
}

// Time-based statistics models
public class DayOfWeekStats
{
    public DayOfWeek DayOfWeek { get; set; }
    public string DayName { get; set; } = string.Empty;
    public int PlayCount { get; set; }
    public double TotalMinutes { get; set; }
    public double AverageMinutesPerOccurrence { get; set; }
}

public class HourOfDayStats
{
    public int Hour { get; set; }
    public string HourLabel { get; set; } = string.Empty;
    public int PlayCount { get; set; }
    public double TotalMinutes { get; set; }
    public double AverageMinutesPerOccurrence { get; set; }
}

public class MonthlyStats
{
    public int Month { get; set; }
    public int Year { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public string MonthYearLabel { get; set; } = string.Empty;
    public int PlayCount { get; set; }
    public double TotalMinutes { get; set; }
    public double AverageMinutesPerDay { get; set; }
}

public class TimeBasedStats
{
    public List<DayOfWeekStats> DayOfWeekStats { get; set; } = new();
    public List<HourOfDayStats> HourOfDayStats { get; set; } = new();
    public List<MonthlyStats> MonthlyStats { get; set; } = new();
}