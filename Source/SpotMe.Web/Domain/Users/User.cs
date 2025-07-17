namespace SpotMe.Web.Domain.Users;

public sealed class User : Entity
{
    public required string EmailAddress { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
}