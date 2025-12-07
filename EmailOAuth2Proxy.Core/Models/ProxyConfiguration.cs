namespace EmailOAuth2Proxy.Core.Models;

/// <summary>
/// Root configuration model for the Email OAuth 2.0 Proxy
/// </summary>
public class ProxyConfiguration
{
    public List<ServerConfiguration> Servers { get; set; } = new();
    public List<AccountConfiguration> Accounts { get; set; } = new();
    public AdvancedConfiguration Advanced { get; set; } = new();
}

/// <summary>
/// Server configuration (IMAP, POP, SMTP)
/// </summary>
public class ServerConfiguration
{
    public string Name { get; set; } = string.Empty;
    public ServerType Type { get; set; }
    public int LocalPort { get; set; }
    public string ServerAddress { get; set; } = string.Empty;
    public int ServerPort { get; set; }
    public string LocalAddress { get; set; } = "127.0.0.1";
    public bool ServerStartTls { get; set; }
    public bool LocalStartTls { get; set; }
    public string? LocalCertificatePath { get; set; }
    public string? LocalKeyPath { get; set; }
}

/// <summary>
/// Server protocol types
/// </summary>
public enum ServerType
{
    IMAP,
    POP,
    SMTP
}

/// <summary>
/// Account configuration for OAuth 2.0 authentication
/// </summary>
public class AccountConfiguration
{
    public string EmailAddress { get; set; } = string.Empty;
    public string PermissionUrl { get; set; } = string.Empty;
    public string TokenUrl { get; set; } = string.Empty;
    public string OAuth2Scope { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = "http://localhost";
    public string ClientId { get; set; } = string.Empty;
    public string? ClientSecret { get; set; }
    public string? ClientSecretEncrypted { get; set; }
    public OAuth2Flow OAuth2Flow { get; set; } = OAuth2Flow.AuthorizationCode;
    public bool UsePkce { get; set; }
    
    // Token storage
    public string? TokenSalt { get; set; }
    public int? TokenIterations { get; set; }
    public string? AccessToken { get; set; }
    public DateTime? AccessTokenExpiry { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? LastActivity { get; set; }
    
    // Advanced options
    public string? OAuth2Resource { get; set; }
    public string? RedirectListenAddress { get; set; }
    public string? JwtCertificatePath { get; set; }
    public string? JwtKeyPath { get; set; }
}

/// <summary>
/// OAuth 2.0 flow types supported
/// </summary>
public enum OAuth2Flow
{
    AuthorizationCode,
    ClientCredentials,
    Password,
    Device,
    ServiceAccount
}

/// <summary>
/// Advanced proxy configuration options
/// </summary>
public class AdvancedConfiguration
{
    public bool DeleteAccountTokenOnPasswordError { get; set; } = true;
    public bool EncryptClientSecretOnFirstUse { get; set; } = false;
    public bool UseLoginPasswordAsClientCredentialsSecret { get; set; } = false;
    public bool AllowCatchAllAccounts { get; set; } = false;
}
