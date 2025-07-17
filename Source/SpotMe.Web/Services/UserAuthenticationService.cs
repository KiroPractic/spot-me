using Microsoft.EntityFrameworkCore;
using SpotMe.Web.Domain.Users;
using SpotMe.Web.Persistency;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace SpotMe.Web.Services;

public class UserAuthenticationService
{
    private readonly DatabaseContext _context;
    private readonly PasswordHashingService _passwordHashingService;
    private readonly CustomAuthenticationService _authService;

    public UserAuthenticationService(
        DatabaseContext context, 
        PasswordHashingService passwordHashingService,
        CustomAuthenticationService authService)
    {
        _context = context;
        _passwordHashingService = passwordHashingService;
        _authService = authService;
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

    public async Task<(bool Success, string Message)> SignInUserAsync(string email, string password)
    {
        var authResult = await AuthenticateUserAsync(email, password);
        
        if (!authResult.Success || authResult.User == null)
        {
            return (false, authResult.Message);
        }

        var signInSuccess = await _authService.SignInAsync(authResult.User.EmailAddress, authResult.User.Id.ToString());
        
        if (signInSuccess)
        {
            return (true, "Sign in successful");
        }
        
        return (false, "Failed to sign in user");
    }

    public async Task<(bool Success, string Message)> RegisterAndSignInUserAsync(string email, string password)
    {
        var registerResult = await RegisterUserAsync(email, password);
        
        if (!registerResult.Success || registerResult.User == null)
        {
            return (false, registerResult.Message);
        }

        var signInSuccess = await _authService.SignInAsync(registerResult.User.EmailAddress, registerResult.User.Id.ToString());
        
        if (signInSuccess)
        {
            return (true, "Registration and sign in successful");
        }
        
        return (false, "User registered but failed to sign in");
    }

    public async Task<bool> SignOutUserAsync()
    {
        return await _authService.SignOutAsync();
    }



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