using Scrobbler.Util;

namespace Scrobbler;

public class ScrobblingService(ILogger<ScrobblingService> _logger, Settings _settings) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            
            await Task.Delay(1000, stoppingToken);
        }
    }
}