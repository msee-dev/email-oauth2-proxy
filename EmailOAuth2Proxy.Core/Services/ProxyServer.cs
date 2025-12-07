using EmailOAuth2Proxy.Core.Models;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace EmailOAuth2Proxy.Core.Services;

/// <summary>
/// Base proxy server that handles connections for IMAP, POP, or SMTP
/// </summary>
public class ProxyServer
{
    private readonly ServerConfiguration _config;
    private readonly ILogger? _logger;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _listenTask;

    public ProxyServer(ServerConfiguration config, ILogger? logger = null)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Start the proxy server
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        var ipAddress = IPAddress.Parse(_config.LocalAddress);
        _listener = new TcpListener(ipAddress, _config.LocalPort);
        _listener.Start();
        
        _logger?.LogInformation($"Proxy server {_config.Name} listening on {_config.LocalAddress}:{_config.LocalPort}");
        
        _listenTask = Task.Run(async () => await AcceptClientsAsync(_cts.Token), _cts.Token);
    }

    /// <summary>
    /// Stop the proxy server
    /// </summary>
    public async Task StopAsync()
    {
        _cts?.Cancel();
        _listener?.Stop();
        
        if (_listenTask != null)
        {
            try
            {
                await _listenTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelling
            }
        }
        
        _logger?.LogInformation($"Proxy server {_config.Name} stopped");
    }

    /// <summary>
    /// Accept incoming client connections
    /// </summary>
    private async Task AcceptClientsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var client = await _listener!.AcceptTcpClientAsync(cancellationToken);
                _ = Task.Run(async () => await HandleClientAsync(client, cancellationToken), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error accepting client connection");
            }
        }
    }

    /// <summary>
    /// Handle a single client connection
    /// </summary>
    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using var clientConnection = client;
        
        try
        {
            _logger?.LogDebug($"Client connected to {_config.Name}");
            
            // Connect to remote server
            using var serverConnection = new TcpClient();
            await serverConnection.ConnectAsync(_config.ServerAddress, _config.ServerPort, cancellationToken);
            
            // Get client stream (with optional local TLS)
            Stream clientStream = clientConnection.GetStream();
            if (_config.LocalStartTls && _config.LocalCertificatePath != null)
            {
                var certificate = new X509Certificate2(_config.LocalCertificatePath, _config.LocalKeyPath);
                var sslStream = new SslStream(clientStream, false);
                await sslStream.AuthenticateAsServerAsync(certificate);
                clientStream = sslStream;
            }
            
            // Get server stream (with TLS)
            Stream serverStream = serverConnection.GetStream();
            var serverSslStream = new SslStream(serverStream, false, ValidateServerCertificate);
            
            if (_config.ServerStartTls)
            {
                // For SMTP with STARTTLS, we need to handle the negotiation
                await HandleStartTlsAsync(clientStream, serverStream, serverSslStream, cancellationToken);
            }
            else
            {
                // Implicit TLS
                await serverSslStream.AuthenticateAsClientAsync(_config.ServerAddress);
                serverStream = serverSslStream;
            }
            
            // Proxy data between client and server
            var clientToServer = ProxyDataAsync(clientStream, serverStream, "Client->Server", cancellationToken);
            var serverToClient = ProxyDataAsync(serverStream, clientStream, "Server->Client", cancellationToken);
            
            await Task.WhenAny(clientToServer, serverToClient);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error handling client for {_config.Name}");
        }
    }

    /// <summary>
    /// Handle STARTTLS negotiation for SMTP
    /// </summary>
    private async Task HandleStartTlsAsync(Stream clientStream, Stream serverStream, SslStream serverSslStream, CancellationToken cancellationToken)
    {
        // This is a simplified implementation
        // In production, you'd need to parse SMTP commands and inject OAuth authentication
        var buffer = new byte[8192];
        
        // Read server greeting
        var bytesRead = await serverStream.ReadAsync(buffer, cancellationToken);
        await clientStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
        
        // Wait for STARTTLS command from client
        // Then upgrade both connections
        // This is a placeholder - full implementation would parse SMTP protocol
    }

    /// <summary>
    /// Proxy data from source to destination stream
    /// </summary>
    private async Task ProxyDataAsync(Stream source, Stream destination, string direction, CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var bytesRead = await source.ReadAsync(buffer, cancellationToken);
                if (bytesRead == 0) break;
                
                _logger?.LogTrace($"{direction}: {bytesRead} bytes");
                
                await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, $"Connection closed: {direction}");
        }
    }

    /// <summary>
    /// Validate server certificate
    /// </summary>
    private bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        // In production, you might want to add additional validation
        return sslPolicyErrors == SslPolicyErrors.None;
    }
}

// Minimal logger interface for when ILogger is not available
public interface ILogger
{
    void LogInformation(string message);
    void LogError(Exception exception, string message);
    void LogError(string message);
    void LogDebug(string message);
    void LogDebug(Exception exception, string message);
    void LogTrace(string message);
}
