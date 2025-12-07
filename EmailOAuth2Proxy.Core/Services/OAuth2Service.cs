using EmailOAuth2Proxy.Core.Models;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using System.Web;

namespace EmailOAuth2Proxy.Core.Services;

/// <summary>
/// Service for handling OAuth 2.0 authentication flows
/// </summary>
public class OAuth2Service
{
    private readonly HttpClient _httpClient;
    private readonly ILogger? _logger;

    public OAuth2Service(ILogger? logger = null)
    {
        _httpClient = new HttpClient();
        _logger = logger;
    }

    /// <summary>
    /// Generate authorization URL for user to visit
    /// </summary>
    public string GenerateAuthorizationUrl(AccountConfiguration account, out string state, out string? codeVerifier)
    {
        state = GenerateRandomString(32);
        codeVerifier = null;

        var parameters = new Dictionary<string, string>
        {
            { "client_id", account.ClientId },
            { "response_type", "code" },
            { "redirect_uri", account.RedirectUri },
            { "scope", account.OAuth2Scope },
            { "state", state }
        };

        // Add PKCE if enabled
        if (account.UsePkce)
        {
            codeVerifier = GenerateRandomString(64);
            var codeChallenge = GenerateCodeChallenge(codeVerifier);
            parameters.Add("code_challenge", codeChallenge);
            parameters.Add("code_challenge_method", "S256");
        }

        var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
        return $"{account.PermissionUrl}?{queryString}";
    }

    /// <summary>
    /// Exchange authorization code for access token
    /// </summary>
    public async Task<TokenResponse?> ExchangeCodeForTokenAsync(AccountConfiguration account, string code, string? codeVerifier = null)
    {
        var parameters = new Dictionary<string, string>
        {
            { "client_id", account.ClientId },
            { "grant_type", "authorization_code" },
            { "code", code },
            { "redirect_uri", account.RedirectUri }
        };

        // Add client secret if present
        if (!string.IsNullOrEmpty(account.ClientSecret))
        {
            parameters.Add("client_secret", account.ClientSecret);
        }

        // Add code verifier for PKCE
        if (!string.IsNullOrEmpty(codeVerifier))
        {
            parameters.Add("code_verifier", codeVerifier);
        }

        return await RequestTokenAsync(account.TokenUrl, parameters);
    }

    /// <summary>
    /// Refresh an access token using a refresh token
    /// </summary>
    public async Task<TokenResponse?> RefreshTokenAsync(AccountConfiguration account, string refreshToken)
    {
        var parameters = new Dictionary<string, string>
        {
            { "client_id", account.ClientId },
            { "grant_type", "refresh_token" },
            { "refresh_token", refreshToken }
        };

        // Add client secret if present
        if (!string.IsNullOrEmpty(account.ClientSecret))
        {
            parameters.Add("client_secret", account.ClientSecret);
        }

        if (!string.IsNullOrEmpty(account.OAuth2Scope))
        {
            parameters.Add("scope", account.OAuth2Scope);
        }

        return await RequestTokenAsync(account.TokenUrl, parameters);
    }

    /// <summary>
    /// Get token using client credentials grant
    /// </summary>
    public async Task<TokenResponse?> GetClientCredentialsTokenAsync(AccountConfiguration account)
    {
        var parameters = new Dictionary<string, string>
        {
            { "client_id", account.ClientId },
            { "client_secret", account.ClientSecret ?? string.Empty },
            { "grant_type", "client_credentials" },
            { "scope", account.OAuth2Scope }
        };

        return await RequestTokenAsync(account.TokenUrl, parameters);
    }

    /// <summary>
    /// Get token using resource owner password credentials grant
    /// </summary>
    public async Task<TokenResponse?> GetPasswordTokenAsync(AccountConfiguration account, string username, string password)
    {
        var parameters = new Dictionary<string, string>
        {
            { "client_id", account.ClientId },
            { "grant_type", "password" },
            { "username", username },
            { "password", password }
        };

        if (!string.IsNullOrEmpty(account.ClientSecret))
        {
            parameters.Add("client_secret", account.ClientSecret);
        }

        if (!string.IsNullOrEmpty(account.OAuth2Scope))
        {
            parameters.Add("scope", account.OAuth2Scope);
        }
        else if (!string.IsNullOrEmpty(account.OAuth2Resource))
        {
            parameters.Add("resource", account.OAuth2Resource);
        }

        return await RequestTokenAsync(account.TokenUrl, parameters);
    }

    /// <summary>
    /// Start device authorization flow
    /// </summary>
    public async Task<DeviceAuthorizationResponse?> StartDeviceAuthorizationAsync(AccountConfiguration account)
    {
        var parameters = new Dictionary<string, string>
        {
            { "client_id", account.ClientId },
            { "scope", account.OAuth2Scope }
        };

        var content = new FormUrlEncodedContent(parameters);
        
        try
        {
            var response = await _httpClient.PostAsync(account.PermissionUrl, content);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<DeviceAuthorizationResponse>(json, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to start device authorization");
            return null;
        }
    }

    /// <summary>
    /// Poll for device authorization token
    /// </summary>
    public async Task<TokenResponse?> PollDeviceAuthorizationAsync(AccountConfiguration account, string deviceCode, int interval = 5)
    {
        var parameters = new Dictionary<string, string>
        {
            { "client_id", account.ClientId },
            { "grant_type", "urn:ietf:params:oauth:grant-type:device_code" },
            { "device_code", deviceCode }
        };

        if (!string.IsNullOrEmpty(account.ClientSecret))
        {
            parameters.Add("client_secret", account.ClientSecret);
        }

        // Poll until we get a token or error
        while (true)
        {
            await Task.Delay(interval * 1000);
            
            var token = await RequestTokenAsync(account.TokenUrl, parameters);
            if (token != null)
            {
                return token;
            }
            
            // Continue polling on "authorization_pending" error
            // Other errors should break the loop
        }
    }

    /// <summary>
    /// Make token request
    /// </summary>
    private async Task<TokenResponse?> RequestTokenAsync(string tokenUrl, Dictionary<string, string> parameters)
    {
        var content = new FormUrlEncodedContent(parameters);
        
        try
        {
            var response = await _httpClient.PostAsync(tokenUrl, content);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
            
            if (tokenResponse != null && tokenResponse.ExpiresIn > 0)
            {
                tokenResponse.ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
            }
            
            return tokenResponse;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to request token");
            return null;
        }
    }

    /// <summary>
    /// Generate cryptographically secure random string
    /// </summary>
    private string GenerateRandomString(int length)
    {
        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_')
            .Substring(0, length);
    }

    /// <summary>
    /// Generate PKCE code challenge from verifier
    /// </summary>
    private string GenerateCodeChallenge(string codeVerifier)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
        return Convert.ToBase64String(hash)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    /// <summary>
    /// Check if token is expired or about to expire
    /// </summary>
    public bool IsTokenExpired(DateTime? expiresAt, int bufferSeconds = 300)
    {
        if (!expiresAt.HasValue)
        {
            return true;
        }
        
        return DateTime.UtcNow.AddSeconds(bufferSeconds) >= expiresAt.Value;
    }
}

/// <summary>
/// OAuth 2.0 token response
/// </summary>
public class TokenResponse
{
    public string? AccessToken { get; set; }
    public string? TokenType { get; set; }
    public int ExpiresIn { get; set; }
    public string? RefreshToken { get; set; }
    public string? Scope { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Device authorization response
/// </summary>
public class DeviceAuthorizationResponse
{
    public string? DeviceCode { get; set; }
    public string? UserCode { get; set; }
    public string? VerificationUri { get; set; }
    public string? VerificationUriComplete { get; set; }
    public int ExpiresIn { get; set; }
    public int Interval { get; set; }
}
