using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpotATrend.Web.Domain;

namespace SpotATrend.Web.Persistency.EntityTypeConfigurations;

public sealed class UploadedFileConfiguration : IEntityTypeConfiguration<UploadedFile>
{
    public void Configure(EntityTypeBuilder<UploadedFile> builder)
    {
        builder.ToTable("UploadedFiles");
        
        // Primary key
        builder.HasKey(x => x.Id);
        
        // User relationship
        builder.Property(x => x.UserId)
            .IsRequired();
        
        // File information
        builder.Property(x => x.FileName)
            .IsRequired()
            .HasMaxLength(500);
            
        builder.Property(x => x.FileHash)
            .IsRequired()
            .HasMaxLength(64); // SHA256 produces 64 character hex string
            
        builder.Property(x => x.FileSize)
            .IsRequired();
            
        builder.Property(x => x.UploadedAt)
            .IsRequired();
        
        // Unique index to prevent uploading the same file twice (same user, same file hash)
        builder.HasIndex(x => new { x.UserId, x.FileHash })
            .IsUnique()
            .HasDatabaseName("IX_UploadedFiles_User_FileHash_Unique");
        
        // Index for querying user's uploads
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_UploadedFiles_UserId");
    }
}

