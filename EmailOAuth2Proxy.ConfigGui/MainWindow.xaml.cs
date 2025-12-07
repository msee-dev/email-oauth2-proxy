using System.Windows;
using System.Windows.Controls;
using System.ServiceProcess;
using System.Diagnostics;
using EmailOAuth2Proxy.Core.Models;
using EmailOAuth2Proxy.Core.Services;
using System.Collections.ObjectModel;

namespace EmailOAuth2Proxy.ConfigGui;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ConfigurationService _configService;
    private ProxyConfiguration? _configuration;
    private ObservableCollection<ServerConfiguration> _servers;
    private ObservableCollection<AccountConfiguration> _accounts;
    private const string ServiceName = "EmailOAuth2ProxyService";

    public MainWindow()
    {
        InitializeComponent();
        
        _configService = new ConfigurationService();
        _servers = new ObservableCollection<ServerConfiguration>();
        _accounts = new ObservableCollection<AccountConfiguration>();
        
        LoadConfiguration();
        UpdateServiceStatus();
    }

    private void LoadConfiguration()
    {
        _configuration = _configService.LoadConfiguration();
        
        // Load servers
        _servers.Clear();
        foreach (var server in _configuration.Servers)
        {
            _servers.Add(server);
        }
        ServersDataGrid.ItemsSource = _servers;
        
        // Load accounts
        _accounts.Clear();
        foreach (var account in _configuration.Accounts)
        {
            _accounts.Add(account);
        }
        AccountsListBox.ItemsSource = _accounts;
        
        // Load advanced settings
        DeleteTokenOnErrorCheckBox.IsChecked = _configuration.Advanced.DeleteAccountTokenOnPasswordError;
        EncryptSecretCheckBox.IsChecked = _configuration.Advanced.EncryptClientSecretOnFirstUse;
        UsePasswordAsSecretCheckBox.IsChecked = _configuration.Advanced.UseLoginPasswordAsClientCredentialsSecret;
        AllowCatchAllCheckBox.IsChecked = _configuration.Advanced.AllowCatchAllAccounts;
        
        ConfigFilePathTextBox.Text = _configService.GetConfigFilePath();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_configuration == null)
            {
                MessageBox.Show("Configuration is not loaded.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Update configuration from UI
            _configuration.Servers = new List<ServerConfiguration>(_servers);
            _configuration.Accounts = new List<AccountConfiguration>(_accounts);
            _configuration.Advanced.DeleteAccountTokenOnPasswordError = DeleteTokenOnErrorCheckBox.IsChecked ?? true;
            _configuration.Advanced.EncryptClientSecretOnFirstUse = EncryptSecretCheckBox.IsChecked ?? false;
            _configuration.Advanced.UseLoginPasswordAsClientCredentialsSecret = UsePasswordAsSecretCheckBox.IsChecked ?? false;
            _configuration.Advanced.AllowCatchAllAccounts = AllowCatchAllCheckBox.IsChecked ?? false;
            
            _configService.SaveConfiguration(_configuration);
            
            MessageBox.Show("Configuration saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void AddServerButton_Click(object sender, RoutedEventArgs e)
    {
        var newServer = new ServerConfiguration
        {
            Name = $"NewServer-{_servers.Count + 1}",
            Type = ServerType.IMAP,
            LocalPort = 1993,
            LocalAddress = "127.0.0.1",
            ServerAddress = "outlook.office365.com",
            ServerPort = 993
        };
        _servers.Add(newServer);
    }

    private void RemoveServerButton_Click(object sender, RoutedEventArgs e)
    {
        if (ServersDataGrid.SelectedItem is ServerConfiguration server)
        {
            _servers.Remove(server);
        }
    }

    private void AddAccountButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AccountEditDialog();
        if (dialog.ShowDialog() == true && dialog.Account != null)
        {
            _accounts.Add(dialog.Account);
        }
    }

    private void EditAccountButton_Click(object sender, RoutedEventArgs e)
    {
        if (AccountsListBox.SelectedItem is AccountConfiguration account)
        {
            var dialog = new AccountEditDialog(account);
            if (dialog.ShowDialog() == true && dialog.Account != null)
            {
                var index = _accounts.IndexOf(account);
                _accounts[index] = dialog.Account;
            }
        }
    }

    private void RemoveAccountButton_Click(object sender, RoutedEventArgs e)
    {
        if (AccountsListBox.SelectedItem is AccountConfiguration account)
        {
            _accounts.Remove(account);
        }
    }

    private void AccountsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        EditAccountButton.IsEnabled = AccountsListBox.SelectedItem != null;
        AuthenticateAccountButton.IsEnabled = AccountsListBox.SelectedItem != null;
        RemoveAccountButton.IsEnabled = AccountsListBox.SelectedItem != null;
    }

    private async void AuthenticateAccountButton_Click(object sender, RoutedEventArgs e)
    {
        if (AccountsListBox.SelectedItem is not AccountConfiguration account)
        {
            return;
        }

        try
        {
            // Validate account configuration
            if (string.IsNullOrWhiteSpace(account.ClientId))
            {
                MessageBox.Show("Account must have a Client ID configured before authentication.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(account.PermissionUrl) || string.IsNullOrWhiteSpace(account.TokenUrl))
            {
                MessageBox.Show("Account must have Permission URL and Token URL configured.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Disable the button during authentication
            AuthenticateAccountButton.IsEnabled = false;
            AuthenticateAccountButton.Content = "Authenticating...";

            // Create services
            var logger = new GuiLogger();
            var oauth2Service = new OAuth2Service(logger);
            var browserService = new OAuth2BrowserService(oauth2Service, logger);

            // Start authentication
            MessageBox.Show(
                "Your default browser will open for authentication.\n\n" +
                "After logging in and granting permissions, the browser will show a success message.\n\n" +
                "You can then close the browser window and return here.",
                "Browser Authentication",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            var tokenResponse = await browserService.AuthenticateAsync(account);

            if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                // Update account with tokens
                account.AccessToken = tokenResponse.AccessToken;
                account.RefreshToken = tokenResponse.RefreshToken;
                account.AccessTokenExpiry = tokenResponse.ExpiresAt;
                account.LastActivity = DateTime.UtcNow;

                // Save configuration
                if (_configuration != null)
                {
                    _configuration.Accounts = new List<AccountConfiguration>(_accounts);
                    _configService.SaveConfiguration(_configuration);
                }

                MessageBox.Show(
                    $"Successfully authenticated account: {account.EmailAddress}\n\n" +
                    $"Access token obtained and saved.\n" +
                    $"Token expires: {tokenResponse.ExpiresAt?.ToLocalTime():g}",
                    "Authentication Successful",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(
                    "Authentication failed. Please check:\n\n" +
                    "1. Your OAuth client credentials are correct\n" +
                    "2. The redirect URI matches your OAuth app configuration\n" +
                    "3. You granted all required permissions\n" +
                    "4. Check the error log for more details",
                    "Authentication Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error during authentication: {ex.Message}\n\n" +
                "Please check your configuration and try again.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            // Re-enable the button
            AuthenticateAccountButton.IsEnabled = AccountsListBox.SelectedItem != null;
            AuthenticateAccountButton.Content = "Authenticate Account";
        }
    }

    private void InstallServiceButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var servicePath = GetServiceExecutablePath();
            if (string.IsNullOrEmpty(servicePath))
            {
                MessageBox.Show("Service executable not found. Please build the service first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"create {ServiceName} binPath= \"{servicePath}\" start= auto",
                Verb = "runas",
                UseShellExecute = true
            };

            var process = Process.Start(startInfo);
            process?.WaitForExit();

            if (process?.ExitCode == 0)
            {
                MessageBox.Show("Service installed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                UpdateServiceStatus();
            }
            else
            {
                MessageBox.Show("Failed to install service. Make sure you have administrator privileges.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error installing service: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UninstallServiceButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"delete {ServiceName}",
                Verb = "runas",
                UseShellExecute = true
            };

            var process = Process.Start(startInfo);
            process?.WaitForExit();

            if (process?.ExitCode == 0)
            {
                MessageBox.Show("Service uninstalled successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                UpdateServiceStatus();
            }
            else
            {
                MessageBox.Show("Failed to uninstall service. Make sure you have administrator privileges.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error uninstalling service: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void StartServiceButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var service = new ServiceController(ServiceName);
            if (service.Status != ServiceControllerStatus.Running)
            {
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                MessageBox.Show("Service started successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                UpdateServiceStatus();
            }
            else
            {
                MessageBox.Show("Service is already running.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error starting service: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void StopServiceButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var service = new ServiceController(ServiceName);
            if (service.Status != ServiceControllerStatus.Stopped)
            {
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                MessageBox.Show("Service stopped successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                UpdateServiceStatus();
            }
            else
            {
                MessageBox.Show("Service is already stopped.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error stopping service: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateServiceStatus()
    {
        try
        {
            var service = new ServiceController(ServiceName);
            ServiceStatusTextBlock.Text = $"Service Status: {service.Status}";
        }
        catch
        {
            ServiceStatusTextBlock.Text = "Service Status: Not Installed";
        }
    }

    private string GetServiceExecutablePath()
    {
        // Try to find the service executable in common locations
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var possiblePaths = new[]
        {
            Path.Combine(baseDir, "..", "EmailOAuth2Proxy.Service", "bin", "Debug", "net8.0-windows", "win-x64", "EmailOAuth2Proxy.Service.exe"),
            Path.Combine(baseDir, "..", "EmailOAuth2Proxy.Service", "bin", "Release", "net8.0-windows", "win-x64", "EmailOAuth2Proxy.Service.exe"),
            Path.Combine(baseDir, "EmailOAuth2Proxy.Service.exe")
        };

        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return string.Empty;
    }
}

/// <summary>
/// Simple logger implementation for GUI
/// </summary>
internal class GuiLogger : Core.Services.ILogger
{
    public void LogInformation(string message)
    {
        Debug.WriteLine($"[INFO] {message}");
    }

    public void LogError(Exception exception, string message)
    {
        Debug.WriteLine($"[ERROR] {message}: {exception.Message}");
    }

    public void LogError(string message)
    {
        Debug.WriteLine($"[ERROR] {message}");
    }

    public void LogDebug(string message)
    {
        Debug.WriteLine($"[DEBUG] {message}");
    }

    public void LogDebug(Exception exception, string message)
    {
        Debug.WriteLine($"[DEBUG] {message}: {exception.Message}");
    }

    public void LogTrace(string message)
    {
        Debug.WriteLine($"[TRACE] {message}");
    }
}
