using EmailOAuth2Proxy.Core.Services;

namespace EmailOAuth2Proxy.Service;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private ProxyManager? _proxyManager;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Email OAuth 2.0 Proxy Service starting at: {time}", DateTimeOffset.Now);
            
            // Initialize configuration service and proxy manager
            var configService = new ConfigurationService();
            _proxyManager = new ProxyManager(configService, new ProxyLogger(_logger));
            
            // Start the proxy servers
            await _proxyManager.StartAsync(stoppingToken);
            
            _logger.LogInformation("Email OAuth 2.0 Proxy Service started successfully");
            
            // Keep the service running until cancellation is requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Email OAuth 2.0 Proxy Service is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in Email OAuth 2.0 Proxy Service");
            throw;
        }
        finally
        {
            if (_proxyManager != null)
            {
                await _proxyManager.StopAsync();
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email OAuth 2.0 Proxy Service is stopping");
        
        if (_proxyManager != null)
        {
            await _proxyManager.StopAsync();
        }
        
        await base.StopAsync(cancellationToken);
    }
}

/// <summary>
/// Adapter to use Microsoft.Extensions.Logging.ILogger with Core.Services.ILogger
/// </summary>
internal class ProxyLogger : Core.Services.ILogger
{
    private readonly Microsoft.Extensions.Logging.ILogger _logger;

    public ProxyLogger(Microsoft.Extensions.Logging.ILogger logger)
    {
        _logger = logger;
    }

    public void LogInformation(string message) => _logger.LogInformation(message);
    public void LogError(Exception exception, string message) => _logger.LogError(exception, message);
    public void LogError(string message) => _logger.LogError(message);
    public void LogDebug(string message) => _logger.LogDebug(message);
    public void LogDebug(Exception exception, string message) => _logger.LogDebug(exception, message);
    public void LogTrace(string message) => _logger.LogTrace(message);
}
