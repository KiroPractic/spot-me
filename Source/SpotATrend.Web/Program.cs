using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SpotATrend.Web.Persistency;
using SpotATrend.Web.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddHttpClient();

// Add CORS services (only needed in development when frontend runs separately)
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });
}

// Configure JWT Authentication
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "SpotATrend";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "SpotATrendUsers";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Configure JSON serialization
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// Register services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<PasswordHashingService>();
builder.Services.AddScoped<UserAuthenticationService>();
builder.Services.AddScoped<DatabaseStatsService>();
builder.Services.AddScoped<StreamingHistoryImportService>();
builder.Services.AddScoped<UserDataService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddMemoryCache();

// Database
var connectionString = builder.Configuration.GetConnectionString("Database");
builder.Services.AddDbContext<DatabaseContext>(o => o.UseNpgsql(connectionString));

// FastEndpoints with Swagger
builder.Services
    .AddFastEndpoints()
    .SwaggerDocument(o =>
    {
        o.DocumentSettings = s =>
        {
            s.DocumentName = "v1";
            s.Title = "SpotATrend API";
            s.Version = "v1";
        };
        o.EnableJWTBearerAuth = true;
    });

var app = builder.Build();

// Auto-apply migrations and setup directories
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

    try
    {
        // Apply any pending migrations automatically
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            logger.LogInformation("Applying {Count} pending migration(s): {Migrations}", 
                pendingMigrations.Count(), string.Join(", ", pendingMigrations));
        }
        
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying database migrations");
        throw; // Re-throw to prevent app from starting with an invalid database state
    }

    // Ensure UserData directory exists
    var userDataPath = Path.Combine(environment.ContentRootPath, "UserData");
    if (!Directory.Exists(userDataPath))
    {
        Directory.CreateDirectory(userDataPath);
        logger.LogInformation("Created UserData directory at: {Path}", userDataPath);
    }
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// CORS (only in development - not needed in production since same domain)
if (app.Environment.IsDevelopment())
{
    app.UseCors();
}

// Authentication and Authorization
app.UseAuthentication();
app.UseAuthorization();

// FastEndpoints
app.UseFastEndpoints(c =>
{
    c.Endpoints.RoutePrefix = "api";
});

// Swagger UI (only in development)
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerGen(); // Must be called after UseFastEndpoints
}

// Health check endpoint
app.MapGet("/health", () => "Healthy");

// SPA fallback: serve index.html for client-side routing (production only)
// In development, frontend runs separately via SvelteKit dev server
if (!app.Environment.IsDevelopment())
{
    app.MapFallbackToFile("index.html");
}

app.Run();
