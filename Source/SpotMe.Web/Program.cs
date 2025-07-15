using SpotMe.Web.Components;
using SpotMe.Web.Services;
using Microsoft.JSInterop;
using System.Text.Json.Serialization;
using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;

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

// Add CORS services
builder.Services.AddCors();

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

// Register Stats service
builder.Services.AddScoped<StatsService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// Enable CORS for Spotify API
app.UseCors(builder => builder
    .WithOrigins("https://accounts.spotify.com", "https://api.spotify.com")
    .AllowAnyMethod()
    .AllowAnyHeader());

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
