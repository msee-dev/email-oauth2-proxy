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
        RemoveAccountButton.IsEnabled = AccountsListBox.SelectedItem != null;
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
