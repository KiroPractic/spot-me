using Microsoft.EntityFrameworkCore;
using SpotATrend.Web.Domain.Users;
using SpotATrend.Web.Persistency;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace SpotATrend.Web.Services;

public class UserAuthenticationService
{
    private readonly DatabaseContext _context;
    private readonly PasswordHashingService _passwordHashingService;

    public UserAuthenticationService(
        DatabaseContext context, 
        PasswordHashingService passwordHashingService)
    {
        _context = context;
        _passwordHashingService = passwordHashingService;
    }

    public async Task<(bool Success, string Message, User? User)> RegisterUserAsync(string email, string password)
    {
        // Validate email format
        if (!IsValidEmail(email))
        {
            return (false, "Invalid email format", null);
        }

        // Validate password requirements
        var passwordValidation = ValidatePassword(password);
        if (!passwordValidation.IsValid)
        {
            return (false, passwordValidation.Message, null);
        }

        // Check if user already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.EmailAddress.ToLower() == email.ToLower());

        if (existingUser != null)
        {
            return (false, "A user with this email already exists", null);
        }

        // Create new user
        var user = new User
        {
            EmailAddress = email,
            PasswordHash = _passwordHashingService.HashPassword(password)
        };

        // Save to database
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return (true, "User registered successfully", user);
    }



    public async Task<(bool Success, string Message, User? User)> AuthenticateUserAsync(string email, string password)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.EmailAddress.ToLower() == email.ToLower());

        if (user == null)
        {
            return (false, "Invalid email or password", null);
        }

        if (!_passwordHashingService.VerifyPassword(password, user.PasswordHash))
        {
            return (false, "Invalid email or password", null);
        }

        return (true, "Authentication successful", user);
    }

    // These methods are no longer needed with JWT - tokens are handled by endpoints



    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.EmailAddress.ToLower() == email.ToLower());
    }

    public (bool IsValid, string Message) ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return (false, "Password is required");
        }

        if (password.Length < 8)
        {
            return (false, "Password must be at least 8 characters long");
        }

        if (!Regex.IsMatch(password, @"[A-Z]"))
        {
            return (false, "Password must contain at least one uppercase letter");
        }

        if (!Regex.IsMatch(password, @"[a-z]"))
        {
            return (false, "Password must contain at least one lowercase letter");
        }

        if (!Regex.IsMatch(password, @"\d"))
        {
            return (false, "Password must contain at least one digit");
        }

        if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]"))
        {
            return (false, "Password must contain at least one special character");
        }

        return (true, "Password is valid");
    }

    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var emailValidation = new EmailAddressAttribute();
            return emailValidation.IsValid(email);
        }
        catch
        {
            return false;
        }
    }
} 