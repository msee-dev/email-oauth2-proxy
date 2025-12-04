using EmailOAuth2Proxy.Core.Models;

namespace EmailOAuth2Proxy.Core.Services;

/// <summary>
/// Manages multiple proxy servers and configuration
/// </summary>
public class ProxyManager
{
    private readonly ConfigurationService _configService;
    private readonly List<ProxyServer> _servers = new();
    private ProxyConfiguration? _configuration;
    private ILogger? _logger;

    public ProxyManager(ConfigurationService configService, ILogger? logger = null)
    {
        _configService = configService;
        _logger = logger;
    }

    /// <summary>
    /// Load configuration and start all proxy servers
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _configuration = _configService.LoadConfiguration();
        _logger?.LogInformation($"Loaded configuration from {_configService.GetConfigFilePath()}");
        
        foreach (var serverConfig in _configuration.Servers)
        {
            try
            {
                var server = new ProxyServer(serverConfig, _logger);
                await server.StartAsync(cancellationToken);
                _servers.Add(server);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Failed to start proxy server {serverConfig.Name}");
            }
        }
        
        _logger?.LogInformation($"Started {_servers.Count} proxy servers");
    }

    /// <summary>
    /// Stop all proxy servers
    /// </summary>
    public async Task StopAsync()
    {
        foreach (var server in _servers)
        {
            await server.StopAsync();
        }
        
        _servers.Clear();
        _logger?.LogInformation("All proxy servers stopped");
    }

    /// <summary>
    /// Reload configuration and restart servers
    /// </summary>
    public async Task ReloadConfigurationAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Reloading configuration...");
        await StopAsync();
        await StartAsync(cancellationToken);
    }

    /// <summary>
    /// Get current configuration
    /// </summary>
    public ProxyConfiguration? GetConfiguration() => _configuration;

    /// <summary>
    /// Update configuration and save to file
    /// </summary>
    public void UpdateConfiguration(ProxyConfiguration configuration)
    {
        _configService.SaveConfiguration(configuration);
        _configuration = configuration;
        _logger?.LogInformation("Configuration saved");
    }
}
