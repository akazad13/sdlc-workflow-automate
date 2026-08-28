using NanoLink.Api.Storage;

namespace NanoLink.Api.Services;

public class UrlCleanupBackgroundService : BackgroundService
{
    private readonly IUrlRepository _repository;
    private readonly ILogger<UrlCleanupBackgroundService> _logger;
    private readonly TimeSpan _checkInterval;

    public UrlCleanupBackgroundService(
        IUrlRepository repository,
        ILogger<UrlCleanupBackgroundService> logger,
        TimeSpan? checkInterval = null)
    {
        _repository = repository;
        _logger = logger;
        _checkInterval = checkInterval ?? TimeSpan.FromSeconds(15);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("UrlCleanupBackgroundService is starting with interval {Interval}s.", _checkInterval.TotalSeconds);

        using var timer = new PeriodicTimer(_checkInterval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                int removed = await _repository.CleanupExpiredAsync(stoppingToken);
                if (removed > 0)
                {
                    _logger.LogInformation("Purged {Count} expired short URLs from memory.", removed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while executing background expired URL cleanup.");
            }
        }

        _logger.LogInformation("UrlCleanupBackgroundService is stopping.");
    }
}
