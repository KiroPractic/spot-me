using SpotMe.Web.Components;
using SpotMe.Web.Services;
using Microsoft.JSInterop;
using System.Text.Json.Serialization;
using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using Microsoft.EntityFrameworkCore;
using SpotMe.Web.Persistency;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();



// Add Blazorise services
builder.Services
    .AddBlazorise(options =>
    {
        options.Immediate = true;
    })
    .AddBootstrap5Providers()
    .AddFontAwesomeIcons();

// Register HttpClient
builder.Services.AddHttpClient();

// Register HttpContext accessor for Blazor
builder.Services.AddHttpContextAccessor();

// Add CORS services
builder.Services.AddCors();

// Configure Authentication (minimal setup for Blazor)
builder.Services.AddAuthentication();

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Register custom AuthenticationStateProvider
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// Configure Blazorise with license key from configuration
builder.Services.AddBlazorise(options =>
{
    var blazoriseToken = builder.Configuration["Blazorise:ProductToken"];
    if (!string.IsNullOrEmpty(blazoriseToken))
    {
        options.ProductToken = blazoriseToken;
    }
});

// Configure JSON serialization
builder.Services.AddControllers()
    .AddJsonOptions(options => 
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Register Spotify services
builder.Services.AddScoped<SpotifyService>(sp => 
    new SpotifyService(
        sp.GetRequiredService<IJSRuntime>(),
        sp.GetRequiredService<IHttpClientFactory>().CreateClient()
    )
);

// Register Stats services
builder.Services.AddScoped<DatabaseStatsService>(); // Database-based stats service
builder.Services.AddScoped<StreamingHistoryImportService>();

// Register User Data service
builder.Services.AddScoped<UserDataService>();

// Register Authentication services
builder.Services.AddScoped<PasswordHashingService>();
builder.Services.AddScoped<UserAuthenticationService>();
builder.Services.AddScoped<CustomAuthenticationService>();



var connectionString = builder.Configuration.GetConnectionString("Database");
builder.Services.AddDbContext<DatabaseContext>(o => o.UseNpgsql(connectionString));
// TODO builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
// builder.Services.AddValidatorsFromAssemblyContaining<UserValidator>(ServiceLifetime.Singleton);


var app = builder.Build();

// Auto-apply migrations and setup directories
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
    
    try
    {
        // Apply any pending migrations
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying database migrations");
    }
    
    // Ensure UserData directory exists
    var userDataPath = Path.Combine(environment.ContentRootPath, "UserData");
    if (!Directory.Exists(userDataPath))
    {
        Directory.CreateDirectory(userDataPath);
        logger.LogInformation("Created UserData directory at: {Path}", userDataPath);
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();



// Add Authentication and Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Enable CORS for Spotify API
app.UseCors(builder => builder
    .WithOrigins("https://accounts.spotify.com", "https://api.spotify.com")
    .AllowAnyMethod()
    .AllowAnyHeader());

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Health check endpoint
app.MapGet("/health", () => "Healthy");

app.Run();
