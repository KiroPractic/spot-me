using FastEndpoints;
using SpotMe.Web.Models;
using SpotMe.Web.Services;
using System.Security.Claims;

namespace SpotMe.Web.Endpoints.Stats.GetOverview;

public class GetOverviewEndpoint : Endpoint<GetOverviewRequest, GetOverviewResponse>
{
    private readonly DatabaseStatsService _statsService;

    public GetOverviewEndpoint(DatabaseStatsService statsService)
    {
        _statsService = statsService;
    }

    public override void Configure()
    {
        Get("/stats/overview");
    }

    public override async Task HandleAsync(GetOverviewRequest req, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var overview = await _statsService.GetStatsOverviewAsync(req.GetStartDate(), req.GetEndDate(), userId);
        await SendOkAsync(new GetOverviewResponse { Overview = overview }, ct);
    }
}

