using Microsoft.EntityFrameworkCore;
using SpotMe.Web.Domain;
using SpotMe.Web.Domain.Users;
using SpotMe.Web.Services;
using SpotMe.Web.Persistency.EntityTypeConfigurations;

namespace SpotMe.Web.Persistency;

public sealed class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
	public DbSet<User> Users { get; init; }
	public DbSet<SpotifyToken> SpotifyTokens { get; init; }
	public DbSet<StreamingHistoryEntry> StreamingHistory { get; init; }
	public DbSet<UploadedFile> UploadedFiles { get; init; }
	
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserConfiguration).Assembly);
	}

	protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
	{
		configurationBuilder.Properties<string>().HaveMaxLength(4000);
	}
}
