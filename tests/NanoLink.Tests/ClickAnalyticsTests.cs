using NanoLink.Api.Services;
using Xunit;

namespace NanoLink.Tests;

public class ClickAnalyticsTests
{
    private readonly ClickAnalyticsService _analytics = new();

    [Fact]
    public void RecordClick_AggregatesReferrersAndBrowsersCorrectly()
    {
        string code = "test-analytics-1";

        _analytics.RecordClick(code, "https://twitter.com/post/1", "Mozilla/5.0 (Windows NT 10.0) AppleWebKit/537.36 Chrome/120.0");
        _analytics.RecordClick(code, "https://twitter.com/post/2", "Mozilla/5.0 (Windows NT 10.0) AppleWebKit/537.36 Chrome/120.0");
        _analytics.RecordClick(code, null, "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) Firefox/121.0");

        var report = _analytics.GetAnalytics(code);

        Assert.Equal(3, report.TotalClicks);
        Assert.True(report.TopReferrers.ContainsKey("twitter.com"));
        Assert.Equal(2, report.TopReferrers["twitter.com"]);
        Assert.True(report.BrowserBreakdown.ContainsKey("Chrome"));
        Assert.Equal(2, report.BrowserBreakdown["Chrome"]);
    }
}
