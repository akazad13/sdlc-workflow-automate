namespace NanoLink.Api.Services;

public record ClickEventRecord(
    string ShortCode,
    DateTime ClickedAtUtc,
    string? Referrer,
    string? UserAgent,
    string Browser,
    string Platform
);

public record UrlAnalyticsResponse(
    string ShortCode,
    int TotalClicks,
    Dictionary<string, int> TopReferrers,
    Dictionary<string, int> BrowserBreakdown
);

public interface IClickAnalyticsService
{
    void RecordClick(string shortCode, string? referrer, string? userAgent);
    UrlAnalyticsResponse GetAnalytics(string shortCode);
}

public class ClickAnalyticsService : IClickAnalyticsService
{
    private readonly List<ClickEventRecord> _events = new();
    private readonly object _lock = new();

    public void RecordClick(string shortCode, string? referrer, string? userAgent)
    {
        string browser = "Other";
        string platform = "Desktop";

        if (!string.IsNullOrEmpty(userAgent))
        {
            if (userAgent.Contains("Chrome")) browser = "Chrome";
            else if (userAgent.Contains("Firefox")) browser = "Firefox";
            else if (userAgent.Contains("Safari")) browser = "Safari";

            if (userAgent.Contains("Mobile") || userAgent.Contains("Android") || userAgent.Contains("iPhone"))
            {
                platform = "Mobile";
            }
        }

        string refHost = "direct";
        if (!string.IsNullOrEmpty(referrer) && Uri.TryCreate(referrer, UriKind.Absolute, out var uri))
        {
            refHost = uri.Host;
        }

        var record = new ClickEventRecord(shortCode, DateTime.UtcNow, refHost, userAgent, browser, platform);
        lock (_lock)
        {
            _events.Add(record);
        }
    }

    public UrlAnalyticsResponse GetAnalytics(string shortCode)
    {
        List<ClickEventRecord> matches;
        lock (_lock)
        {
            matches = _events.Where(e => e.ShortCode == shortCode).ToList();
        }

        var referrers = matches
            .GroupBy(e => e.Referrer ?? "direct")
            .ToDictionary(g => g.Key, g => g.Count());

        var browsers = matches
            .GroupBy(e => e.Browser)
            .ToDictionary(g => g.Key, g => g.Count());

        return new UrlAnalyticsResponse(shortCode, matches.Count, referrers, browsers);
    }
}
