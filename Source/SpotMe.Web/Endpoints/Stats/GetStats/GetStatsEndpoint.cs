using FastEndpoints;
using SpotMe.Web.Models;
using SpotMe.Web.Services;
using System.Security.Claims;

namespace SpotMe.Web.Endpoints.Stats.GetStats;

public class GetStatsEndpoint : Endpoint<GetStatsRequest, GetStatsResponse>
{
    private readonly DatabaseStatsService _statsService;

    public GetStatsEndpoint(DatabaseStatsService statsService)
    {
        _statsService = statsService;
    }

    public override void Configure()
    {
        Get("/stats");
    }

    public override async Task HandleAsync(GetStatsRequest req, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var stats = await _statsService.GetStatsOverviewAsync(req.GetStartDate(), req.GetEndDate(), userId);
        await SendOkAsync(new GetStatsResponse { Stats = stats }, ct);
    }
}

