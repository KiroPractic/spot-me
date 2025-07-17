using System.Security.Cryptography;
using System.Text;

namespace SpotMe.Web.Services;

public class PasswordHashingService
{
    private const int SaltRounds = 12;

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, SaltRounds);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
} 