using SpotMe.Web.Domain;

namespace SpotMe.Web.Services;

public sealed class StreamingHistoryEntry : Entity
{
    // User reference
    public Guid UserId { get; private set; }
    
    // Uploaded file reference
    public Guid UploadedFileId { get; private set; }
    
    // Core playback data
    public DateTime PlayedAt { get; private set; }
    public long MsPlayed { get; private set; }
    public string Platform { get; private set; } = string.Empty;
    public string? PlayedInCountryCode { get; private set; }
    
    // Content identification
    public string ContentType { get; private set; } = string.Empty; // "audio", "video", "podcast"
    public string? SpotifyUri { get; private set; } // spotify:track:xxx or spotify:episode:xxx
    
    // Audio/Music metadata
    public string? TrackName { get; private set; }
    public string? ArtistName { get; private set; }
    public string? AlbumName { get; private set; }
    
    // Podcast metadata
    public string? EpisodeName { get; private set; }
    public string? ShowName { get; private set; }
    
    // Playback behavior
    public bool? Skipped { get; private set; }
    public string? ReasonStart { get; private set; }
    public string? ReasonEnd { get; private set; }
    public bool? Shuffle { get; private set; }
    public bool? Offline { get; private set; }
    
    // Computed fields for performance (mapped as shadow properties)
    public double MinutesPlayed => MsPlayed / 60000.0;
    public DayOfWeek DayOfWeek => PlayedAt.DayOfWeek;
    
    private StreamingHistoryEntry() { } // EF constructor
    
    public StreamingHistoryEntry(
        Guid userId,
        DateTime playedAt,
        long msPlayed,
        string platform,
        string contentType,
        Guid uploadedFileId,
        string? countryCode = null,
        string? spotifyUri = null,
        string? trackName = null,
        string? artistName = null,
        string? albumName = null,
        string? episodeName = null,
        string? showName = null,
        bool? skipped = null,
        string? reasonStart = null,
        string? reasonEnd = null,
        bool? shuffle = null,
        bool? offline = null)
    {
        UserId = userId;
        UploadedFileId = uploadedFileId;
        PlayedAt = playedAt;
        MsPlayed = msPlayed;
        Platform = platform;
        ContentType = contentType;
        PlayedInCountryCode = countryCode;
        SpotifyUri = spotifyUri;
        TrackName = trackName;
        ArtistName = artistName;
        AlbumName = albumName;
        EpisodeName = episodeName;
        ShowName = showName;
        Skipped = skipped;
        ReasonStart = reasonStart;
        ReasonEnd = reasonEnd;
        Shuffle = shuffle;
        Offline = offline;
    }
}

