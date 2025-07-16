using Microsoft.EntityFrameworkCore;

namespace SpotMe.Web.Persistency;

public sealed class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		// modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserConfiguration).Assembly);
	}

	protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
	{
		configurationBuilder.Properties<string>().HaveMaxLength(4000);
	}
}
