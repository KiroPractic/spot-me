using SpotMe.Web.Services;

namespace SpotMe.Web.Services;

public class UserDataService
{
    private readonly CustomAuthenticationService _authService;

    public UserDataService(CustomAuthenticationService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Get the current authenticated user ID
    /// </summary>
    public string GetCurrentUserId()
    {
        var user = _authService.CurrentUser;
        if (!user.Identity?.IsAuthenticated == true)
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User ID not found in claims");
        }

        return userId;
    }
}