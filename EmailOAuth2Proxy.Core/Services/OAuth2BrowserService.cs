using EmailOAuth2Proxy.Core.Models;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace EmailOAuth2Proxy.Core.Services;

/// <summary>
/// Service for handling browser-based OAuth 2.0 authentication
/// </summary>
public class OAuth2BrowserService
{
    private readonly OAuth2Service _oAuth2Service;
    private readonly ILogger? _logger;
    private HttpListener? _httpListener;
    private string? _authorizationCode;
    private string? _state;
    private string? _codeVerifier;
    private TaskCompletionSource<string>? _authorizationTcs;

    public OAuth2BrowserService(OAuth2Service oAuth2Service, ILogger? logger = null)
    {
        _oAuth2Service = oAuth2Service;
        _logger = logger;
    }

    /// <summary>
    /// Start browser-based OAuth authentication flow
    /// </summary>
    /// <param name="account">Account configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token response with access token and refresh token</returns>
    public async Task<TokenResponse?> AuthenticateAsync(AccountConfiguration account, CancellationToken cancellationToken = default)
    {
        try
        {
            // Start local HTTP listener to receive the OAuth callback
            var redirectUri = new Uri(account.RedirectUri);
            var listenerPrefix = redirectUri.Scheme == "http" 
                ? $"http://localhost:{(redirectUri.Port > 0 ? redirectUri.Port : 80)}/"
                : throw new InvalidOperationException("Only HTTP redirect URIs are supported for browser authentication");

            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add(listenerPrefix);
            _httpListener.Start();

            _logger?.LogInformation($"Started HTTP listener on {listenerPrefix}");

            // Generate authorization URL
            var authUrl = _oAuth2Service.GenerateAuthorizationUrl(account, out _state, out _codeVerifier);
            
            _logger?.LogInformation($"Opening browser to: {authUrl}");

            // Open the authorization URL in the default browser
            OpenBrowser(authUrl);

            // Wait for the OAuth callback
            _authorizationTcs = new TaskCompletionSource<string>();
            
            // Start listening for the callback
            _ = Task.Run(async () => await ListenForCallbackAsync(cancellationToken), cancellationToken);

            // Wait for authorization code with timeout
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            
            try
            {
                _authorizationCode = await _authorizationTcs.Task.WaitAsync(linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger?.LogError("Authentication timed out or was cancelled");
                return null;
            }

            if (string.IsNullOrEmpty(_authorizationCode))
            {
                _logger?.LogError("Failed to receive authorization code");
                return null;
            }

            // Exchange authorization code for tokens
            _logger?.LogInformation("Exchanging authorization code for tokens");
            var tokenResponse = await _oAuth2Service.ExchangeCodeForTokenAsync(account, _authorizationCode, _codeVerifier);

            if (tokenResponse != null)
            {
                _logger?.LogInformation("Successfully obtained access token");
            }
            else
            {
                _logger?.LogError("Failed to exchange authorization code for tokens");
            }

            return tokenResponse;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during browser authentication");
            return null;
        }
        finally
        {
            _httpListener?.Stop();
            _httpListener?.Close();
        }
    }

    /// <summary>
    /// Listen for the OAuth callback from the browser
    /// </summary>
    private async Task ListenForCallbackAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && _httpListener != null && _httpListener.IsListening)
            {
                var context = await _httpListener.GetContextAsync();
                
                try
                {
                    // Extract query parameters from the callback URL
                    var queryParams = System.Web.HttpUtility.ParseQueryString(context.Request.Url?.Query ?? "");
                    var code = queryParams["code"];
                    var state = queryParams["state"];
                    var error = queryParams["error"];
                    var errorDescription = queryParams["error_description"];

                    // Check for errors
                    if (!string.IsNullOrEmpty(error))
                    {
                        _logger?.LogError($"OAuth error: {error} - {errorDescription}");
                        await SendResponseAsync(context, false, $"Authentication failed: {error}");
                        _authorizationTcs?.TrySetResult(string.Empty);
                        break;
                    }

                    // Verify state parameter to prevent CSRF attacks
                    if (state != _state)
                    {
                        _logger?.LogError("State parameter mismatch - possible CSRF attack");
                        await SendResponseAsync(context, false, "Authentication failed: Invalid state");
                        _authorizationTcs?.TrySetResult(string.Empty);
                        break;
                    }

                    // Check if we received the authorization code
                    if (!string.IsNullOrEmpty(code))
                    {
                        _logger?.LogInformation("Received authorization code");
                        await SendResponseAsync(context, true, "Authentication successful! You can close this window.");
                        _authorizationTcs?.TrySetResult(code);
                        break;
                    }

                    // Unknown response
                    await SendResponseAsync(context, false, "Authentication failed: No authorization code received");
                    _authorizationTcs?.TrySetResult(string.Empty);
                    break;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error processing OAuth callback");
                    await SendResponseAsync(context, false, "An error occurred during authentication");
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in callback listener");
            _authorizationTcs?.TrySetException(ex);
        }
    }

    /// <summary>
    /// Send HTML response to the browser
    /// </summary>
    private async Task SendResponseAsync(HttpListenerContext context, bool success, string message)
    {
        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Email OAuth 2.0 Proxy - Authentication</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            margin: 0;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        }}
        .container {{
            background: white;
            padding: 40px;
            border-radius: 10px;
            box-shadow: 0 10px 40px rgba(0,0,0,0.2);
            text-align: center;
            max-width: 400px;
        }}
        .icon {{
            font-size: 64px;
            margin-bottom: 20px;
        }}
        .success {{ color: #4CAF50; }}
        .error {{ color: #f44336; }}
        h1 {{
            margin: 0 0 20px 0;
            color: #333;
        }}
        p {{
            color: #666;
            line-height: 1.6;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='icon {(success ? "success" : "error")}'>
            {(success ? "✓" : "✗")}
        </div>
        <h1>{(success ? "Success!" : "Authentication Failed")}</h1>
        <p>{message}</p>
    </div>
</body>
</html>";

        var buffer = Encoding.UTF8.GetBytes(html);
        context.Response.ContentLength64 = buffer.Length;
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.StatusCode = success ? 200 : 400;
        
        await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        context.Response.Close();
    }

    /// <summary>
    /// Open URL in the default browser
    /// </summary>
    private void OpenBrowser(string url)
    {
        try
        {
            // Try different methods to open the browser depending on the platform
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            else if (OperatingSystem.IsLinux())
            {
                Process.Start("xdg-open", url);
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start("open", url);
            }
            else
            {
                _logger?.LogError("Unsupported operating system for opening browser");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to open browser");
            throw;
        }
    }
}
