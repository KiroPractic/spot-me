using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpotMe.Web.Domain.Users;

namespace SpotMe.Web.Persistency.EntityTypeConfigurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasIndex(u => u.EmailAddress).IsUnique();

        builder.OwnsMany(u => u.Files, fileBuilder =>
        {
            fileBuilder.ToTable("UserFiles");
            
            fileBuilder.WithOwner().HasForeignKey("UserId");
            
            fileBuilder.Property(f => f.ObjectId)
                .IsRequired();
                
            fileBuilder.Property(f => f.OriginalFileName)
                .IsRequired()
                .HasMaxLength(255);
                
            fileBuilder.Property(f => f.Extension)
                .IsRequired()
                .HasMaxLength(50);
                
            fileBuilder.Property(f => f.ContentType)
                .IsRequired()
                .HasMaxLength(100);
                
            fileBuilder.Property(f => f.Link)
                .IsRequired()
                .HasMaxLength(500);
                
            fileBuilder.Property(f => f.Title)
                .HasMaxLength(255);
                
            fileBuilder.Property(f => f.Description)
                .HasMaxLength(1000);
                
            fileBuilder.Property(f => f.UpdatedOn)
                .IsRequired();

            fileBuilder.HasIndex(f => f.ObjectId)
                .IsUnique();
        });
    }
}
