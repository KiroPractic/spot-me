using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpotMe.Web.Domain.Users;

namespace SpotMe.Web.Persistency.EntityTypeConfigurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasIndex(u => u.EmailAddress).IsUnique();
    }
}

