using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SpotATrend.Web.Persistency;
using SpotATrend.Web.Services;

namespace SpotATrend.Web.Endpoints.Auth.Login;

public class LoginEndpoint : Endpoint<LoginRequest, LoginResponse>
{
    private readonly DatabaseContext _context;
    private readonly PasswordHashingService _passwordHashingService;
    private readonly JwtService _jwtService;

    public LoginEndpoint(
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
        Post("/auth/login");
        AllowAnonymous();
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.EmailAddress.ToLower() == req.Email.ToLower(), ct);

        if (user == null || !_passwordHashingService.VerifyPassword(req.Password, user.PasswordHash))
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var token = _jwtService.GenerateToken(user.Id, user.EmailAddress);

        await SendOkAsync(new LoginResponse
        {
            Token = token,
            Email = user.EmailAddress,
            UserId = user.Id
        }, ct);
    }
}

public class LoginRequestValidator : Validator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(1);
    }
}

