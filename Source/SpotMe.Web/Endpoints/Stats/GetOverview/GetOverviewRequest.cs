using FastEndpoints;

namespace SpotMe.Web.Endpoints.Stats.GetOverview;

public class GetOverviewRequest
{
    [QueryParam]
    public string? StartDate { get; set; }
    
    [QueryParam]
    public string? EndDate { get; set; }
    
    public DateOnly? GetStartDate()
    {
        return string.IsNullOrEmpty(StartDate) ? null : DateOnly.Parse(StartDate);
    }
    
    public DateOnly? GetEndDate()
    {
        return string.IsNullOrEmpty(EndDate) ? null : DateOnly.Parse(EndDate);
    }
}

