namespace SpotATrend.Web.Endpoints.Auth.Register;

public class RegisterResponse
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string? Message { get; set; }
}

