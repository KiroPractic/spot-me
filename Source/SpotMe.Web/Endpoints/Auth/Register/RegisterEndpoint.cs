using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SpotMe.Web.Domain.Users;
using SpotMe.Web.Persistency;
using SpotMe.Web.Services;
using System.Text.RegularExpressions;

namespace SpotMe.Web.Endpoints.Auth.Register;

public class RegisterEndpoint : Endpoint<RegisterRequest, RegisterResponse>
{
    private readonly DatabaseContext _context;
    private readonly PasswordHashingService _passwordHashingService;
    private readonly JwtService _jwtService;

    public RegisterEndpoint(
        DatabaseContext context,
        PasswordHashingService passwordHashingService,
        JwtService jwtService)
    {
        _context = context;
        _passwordHashingService = passwordHashingService;
        _jwtService = jwtService;
    }

    public override void Configure()
    {
        Post("/auth/register");
        AllowAnonymous();
    }

    public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
    {
        // Validate email format
        if (!IsValidEmail(req.Email))
        {
            await SendAsync(new RegisterResponse(), 400, ct);
            return;
        }

        // Validate password
        var passwordValidation = ValidatePassword(req.Password);
        if (!passwordValidation.IsValid)
        {
            await SendAsync(new RegisterResponse(), 400, ct);
            return;
        }

        // Check if user already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.EmailAddress.ToLower() == req.Email.ToLower(), ct);

        if (existingUser != null)
        {
            await SendAsync(new RegisterResponse(), 409, ct); // Conflict
            return;
        }

        // Create new user
        var user = new User
        {
            EmailAddress = req.Email,
            PasswordHash = _passwordHashingService.HashPassword(req.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);

        var token = _jwtService.GenerateToken(user.Id, user.EmailAddress);

        await SendOkAsync(new RegisterResponse
        {
            Token = token,
            Email = user.EmailAddress,
            UserId = user.Id
        }, ct);
    }

    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var emailValidation = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
            return emailValidation.IsValid(email);
        }
        catch
        {
            return false;
        }
    }

    private (bool IsValid, string Message) ValidatePassword(string password)
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
}

public class RegisterRequestValidator : Validator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8);
    }
}

