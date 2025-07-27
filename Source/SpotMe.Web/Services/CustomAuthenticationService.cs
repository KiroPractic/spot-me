using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.JSInterop;
using System.Text.Json;

namespace SpotMe.Web.Services;

public class CustomAuthenticationService
{
    public event Action<ClaimsPrincipal>? UserChanged;
    private ClaimsPrincipal? currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IJSRuntime _jsRuntime;

    public CustomAuthenticationService(IHttpContextAccessor httpContextAccessor, IJSRuntime jsRuntime)
    {
        _httpContextAccessor = httpContextAccessor;
        _jsRuntime = jsRuntime;
    }

    public ClaimsPrincipal CurrentUser
    {
        get 
        { 
            if (currentUser == null)
            {
                LoadUserFromHttpContext();
            }
            return currentUser ?? new(); 
        }
        private set
        {
            currentUser = value;
            UserChanged?.Invoke(currentUser);
        }
    }

    public async Task<bool> SignInAsync(string email, string userId)
    {
        try
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.NameIdentifier, userId)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // Set authentication state immediately for UI
            CurrentUser = principal;

            // Set cookie via JavaScript to avoid header issues
            var userData = new
            {
                Email = email,
                UserId = userId,
                Name = email
            };
            
            var userDataJson = JsonSerializer.Serialize(userData);
            var encodedUserData = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(userDataJson));
            
            // Set cookie that expires in 30 days
            var cookieOptions = "expires=" + DateTime.UtcNow.AddDays(30).ToString("R") + "; path=/; SameSite=Lax";
            await _jsRuntime.InvokeVoidAsync("eval", $"document.cookie = 'SpotMe.Auth={encodedUserData}; {cookieOptions}'");

            return true;
        }
        catch (Exception ex)
        {
            // Log error but don't throw - we might be in prerendering
            Console.WriteLine($"SignIn error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SignOutAsync()
    {
        try
        {
            CurrentUser = new ClaimsPrincipal(new ClaimsIdentity());

            // Clear cookie via JavaScript
            await _jsRuntime.InvokeVoidAsync("eval", "document.cookie = 'SpotMe.Auth=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;'");
            
            return true;
        }
        catch (Exception ex)
        {
            // Log error but don't throw
            Console.WriteLine($"SignOut error: {ex.Message}");
            return false;
        }
    }

    public void LoadUserFromHttpContext()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            CurrentUser = httpContext.User;
        }
        else
        {
            // Try to load from HTTP context cookies first (works during static rendering)
            LoadUserFromHttpContextCookies();
        }
    }

    private void LoadUserFromHttpContextCookies()
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Request?.Cookies != null && httpContext.Request.Cookies.TryGetValue("SpotMe.Auth", out var cookieValue))
            {
                
                if (!string.IsNullOrEmpty(cookieValue))
                {
                    var userDataJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cookieValue));
                    var userData = JsonSerializer.Deserialize<JsonElement>(userDataJson);
                    
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.Name, userData.GetProperty("Email").GetString() ?? ""),
                        new Claim(ClaimTypes.Email, userData.GetProperty("Email").GetString() ?? ""),
                        new Claim(ClaimTypes.NameIdentifier, userData.GetProperty("UserId").GetString() ?? "")
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    CurrentUser = new ClaimsPrincipal(identity);
                }
                else
                {
                    CurrentUser = new ClaimsPrincipal(new ClaimsIdentity());
                }
            }
            else
            {
                CurrentUser = new ClaimsPrincipal(new ClaimsIdentity());
            }
        }
        catch
        {
            CurrentUser = new ClaimsPrincipal(new ClaimsIdentity());
        }
    }

    private async Task LoadUserFromCookieAsync()
    {
        try
        {
            var cookieValue = await _jsRuntime.InvokeAsync<string>("eval", "document.cookie.split('; ').find(row => row.startsWith('SpotMe.Auth='))?.split('=')[1] || ''");
            
            if (!string.IsNullOrEmpty(cookieValue))
            {
                var userDataJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cookieValue));
                var userData = JsonSerializer.Deserialize<JsonElement>(userDataJson);
                
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, userData.GetProperty("Email").GetString() ?? ""),
                    new Claim(ClaimTypes.Email, userData.GetProperty("Email").GetString() ?? ""),
                    new Claim(ClaimTypes.NameIdentifier, userData.GetProperty("UserId").GetString() ?? "")
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                CurrentUser = new ClaimsPrincipal(identity);
            }
            else
            {
                CurrentUser = new ClaimsPrincipal(new ClaimsIdentity());
            }
        }
        catch
        {
            // If there's any error loading from cookie, just set anonymous user
            CurrentUser = new ClaimsPrincipal(new ClaimsIdentity());
        }
    }

    public async Task InitializeAsync()
    {
        // Initialize authentication state when the service is first used
        LoadUserFromHttpContext();
        
        // If still not authenticated and JavaScript is available, try loading from JavaScript cookies
        if (CurrentUser.Identity?.IsAuthenticated != true)
        {
            try
            {
                await LoadUserFromCookieAsync();
            }
            catch
            {
                // Ignore errors during initialization (likely prerendering)
            }
        }
    }
} 