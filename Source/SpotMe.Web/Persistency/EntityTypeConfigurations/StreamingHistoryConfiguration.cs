using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpotMe.Web.Domain;
using SpotMe.Web.Services;

namespace SpotMe.Web.Persistency.EntityTypeConfigurations;

public sealed class StreamingHistoryConfiguration : IEntityTypeConfiguration<StreamingHistoryEntry>
{
    public void Configure(EntityTypeBuilder<StreamingHistoryEntry> builder)
    {
        builder.ToTable("StreamingHistory");
        
        // Primary key
        builder.HasKey(x => x.Id);
        
        // User relationship
        builder.Property(x => x.UserId)
            .IsRequired();
        
        // Uploaded file relationship with cascade delete
        builder.Property(x => x.UploadedFileId)
            .IsRequired();
        
        builder.HasIndex(x => x.UploadedFileId)
            .HasDatabaseName("IX_StreamingHistory_UploadedFileId");
        
        // Configure relationship with cascade delete
        builder.HasOne<UploadedFile>()
            .WithMany()
            .HasForeignKey(x => x.UploadedFileId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Core playback data
        builder.Property(x => x.PlayedAt)
            .IsRequired()
            .HasColumnType("timestamp without time zone");
            
        builder.Property(x => x.MsPlayed)
            .IsRequired();
            
        builder.Property(x => x.Platform)
            .IsRequired()
            .HasMaxLength(200);
            
        builder.Property(x => x.PlayedInCountryCode)
            .HasMaxLength(5);
        
        // Content identification
        builder.Property(x => x.ContentType)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(x => x.SpotifyUri)
            .HasMaxLength(500);
        
        // Audio metadata
        builder.Property(x => x.TrackName)
            .HasMaxLength(1000);
            
        builder.Property(x => x.ArtistName)
            .HasMaxLength(1000);
            
        builder.Property(x => x.AlbumName)
            .HasMaxLength(1000);
        
        // Podcast metadata
        builder.Property(x => x.EpisodeName)
            .HasMaxLength(1000);
            
        builder.Property(x => x.ShowName)
            .HasMaxLength(1000);
        
        // Playback behavior
        builder.Property(x => x.ReasonStart)
            .HasMaxLength(100);
            
        builder.Property(x => x.ReasonEnd)
            .HasMaxLength(100);
        
        // Performance indexes
        builder.HasIndex(x => new { x.UserId, x.ContentType, x.PlayedAt })
            .HasDatabaseName("IX_StreamingHistory_User_ContentType_PlayedAt");
            
        builder.HasIndex(x => new { x.UserId, x.ArtistName, x.PlayedAt })
            .HasDatabaseName("IX_StreamingHistory_User_Artist_PlayedAt")
            .HasFilter("\"ArtistName\" IS NOT NULL");
            
        builder.HasIndex(x => new { x.UserId, x.TrackName, x.ArtistName })
            .HasDatabaseName("IX_StreamingHistory_User_Track_Artist")
            .HasFilter("\"TrackName\" IS NOT NULL AND \"ArtistName\" IS NOT NULL");
            
        builder.HasIndex(x => x.SpotifyUri)
            .HasDatabaseName("IX_StreamingHistory_SpotifyUri")
            .HasFilter("\"SpotifyUri\" IS NOT NULL");
        
        // Ignore computed properties that don't have backing fields
        builder.Ignore(x => x.MinutesPlayed);
        builder.Ignore(x => x.DayOfWeek);
        
        // Note: Computed columns removed due to PostgreSQL immutability issues
        // HourOfDay and YearMonth will be calculated in application code
    }
}
