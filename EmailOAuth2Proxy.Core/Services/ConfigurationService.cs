using EmailOAuth2Proxy.Core.Models;
using System.Text.Json;

namespace EmailOAuth2Proxy.Core.Services;

/// <summary>
/// Service for loading and saving proxy configuration from/to file
/// </summary>
public class ConfigurationService
{
    private readonly string _configFilePath;
    
    public ConfigurationService(string? configFilePath = null)
    {
        _configFilePath = configFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EmailOAuth2Proxy",
            "emailproxy.config.json");
    }
    
    /// <summary>
    /// Load configuration from file
    /// </summary>
    public ProxyConfiguration LoadConfiguration()
    {
        if (!File.Exists(_configFilePath))
        {
            return CreateDefaultConfiguration();
        }
        
        try
        {
            var json = File.ReadAllText(_configFilePath);
            var config = JsonSerializer.Deserialize<ProxyConfiguration>(json);
            return config ?? CreateDefaultConfiguration();
        }
        catch
        {
            return CreateDefaultConfiguration();
        }
    }
    
    /// <summary>
    /// Save configuration to file
    /// </summary>
    public void SaveConfiguration(ProxyConfiguration configuration)
    {
        var directory = Path.GetDirectoryName(_configFilePath);
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        
        var json = JsonSerializer.Serialize(configuration, options);
        File.WriteAllText(_configFilePath, json);
    }
    
    /// <summary>
    /// Create default configuration with example servers and accounts
    /// </summary>
    private ProxyConfiguration CreateDefaultConfiguration()
    {
        return new ProxyConfiguration
        {
            Servers = new List<ServerConfiguration>
            {
                // Office 365 / Outlook servers
                new ServerConfiguration
                {
                    Name = "IMAP-1993",
                    Type = ServerType.IMAP,
                    LocalPort = 1993,
                    ServerAddress = "outlook.office365.com",
                    ServerPort = 993,
                    LocalAddress = "127.0.0.1"
                },
                new ServerConfiguration
                {
                    Name = "POP-1995",
                    Type = ServerType.POP,
                    LocalPort = 1995,
                    ServerAddress = "outlook.office365.com",
                    ServerPort = 995,
                    LocalAddress = "127.0.0.1"
                },
                new ServerConfiguration
                {
                    Name = "SMTP-1587",
                    Type = ServerType.SMTP,
                    LocalPort = 1587,
                    ServerAddress = "smtp-mail.outlook.com",
                    ServerPort = 587,
                    ServerStartTls = true,
                    LocalAddress = "127.0.0.1"
                },
                // Gmail servers
                new ServerConfiguration
                {
                    Name = "IMAP-2993",
                    Type = ServerType.IMAP,
                    LocalPort = 2993,
                    ServerAddress = "imap.gmail.com",
                    ServerPort = 993,
                    LocalAddress = "127.0.0.1"
                },
                new ServerConfiguration
                {
                    Name = "POP-2995",
                    Type = ServerType.POP,
                    LocalPort = 2995,
                    ServerAddress = "pop.gmail.com",
                    ServerPort = 995,
                    LocalAddress = "127.0.0.1"
                },
                new ServerConfiguration
                {
                    Name = "SMTP-2465",
                    Type = ServerType.SMTP,
                    LocalPort = 2465,
                    ServerAddress = "smtp.gmail.com",
                    ServerPort = 465,
                    LocalAddress = "127.0.0.1"
                }
            },
            Accounts = new List<AccountConfiguration>
            {
                // Office 365 / Outlook example (user needs to fill in details)
                new AccountConfiguration
                {
                    EmailAddress = "your.office365.or.outlook.address@example.com",
                    PermissionUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize",
                    TokenUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/token",
                    OAuth2Scope = "https://outlook.office.com/IMAP.AccessAsUser.All https://outlook.office.com/POP.AccessAsUser.All https://outlook.office.com/SMTP.Send offline_access",
                    RedirectUri = "http://localhost",
                    ClientId = "*** your client id here ***",
                    ClientSecret = "*** your client secret here (remove this entire line if a secret is not required) ***"
                },
                // Gmail example (user needs to fill in details)
                new AccountConfiguration
                {
                    EmailAddress = "your.email@gmail.com",
                    PermissionUrl = "https://accounts.google.com/o/oauth2/auth",
                    TokenUrl = "https://oauth2.googleapis.com/token",
                    OAuth2Scope = "https://mail.google.com/",
                    RedirectUri = "http://localhost",
                    ClientId = "*** your client id here ***",
                    ClientSecret = "*** your client secret here ***"
                }
            },
            Advanced = new AdvancedConfiguration()
        };
    }
    
    public string GetConfigFilePath() => _configFilePath;
}
