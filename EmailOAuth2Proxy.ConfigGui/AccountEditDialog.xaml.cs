using System.Windows;
using EmailOAuth2Proxy.Core.Models;

namespace EmailOAuth2Proxy.ConfigGui;

/// <summary>
/// Dialog for editing account configuration
/// </summary>
public partial class AccountEditDialog : Window
{
    public AccountConfiguration? Account { get; private set; }

    public AccountEditDialog(AccountConfiguration? existingAccount = null)
    {
        InitializeComponent();
        
        if (existingAccount != null)
        {
            Account = new AccountConfiguration
            {
                EmailAddress = existingAccount.EmailAddress,
                PermissionUrl = existingAccount.PermissionUrl,
                TokenUrl = existingAccount.TokenUrl,
                OAuth2Scope = existingAccount.OAuth2Scope,
                RedirectUri = existingAccount.RedirectUri,
                ClientId = existingAccount.ClientId,
                ClientSecret = existingAccount.ClientSecret,
                OAuth2Flow = existingAccount.OAuth2Flow,
                UsePkce = existingAccount.UsePkce
            };
            
            LoadAccountToForm();
        }
        else
        {
            // Default values for new account
            Account = new AccountConfiguration
            {
                RedirectUri = "http://localhost"
            };
        }
    }

    private void LoadAccountToForm()
    {
        if (Account == null) return;

        EmailAddressTextBox.Text = Account.EmailAddress;
        PermissionUrlTextBox.Text = Account.PermissionUrl;
        TokenUrlTextBox.Text = Account.TokenUrl;
        OAuth2ScopeTextBox.Text = Account.OAuth2Scope;
        RedirectUriTextBox.Text = Account.RedirectUri;
        ClientIdTextBox.Text = Account.ClientId;
        ClientSecretTextBox.Text = Account.ClientSecret;
        OAuth2FlowComboBox.SelectedItem = Account.OAuth2Flow;
        UsePkceCheckBox.IsChecked = Account.UsePkce;
    }

    private void SaveAccountFromForm()
    {
        if (Account == null) return;

        Account.EmailAddress = EmailAddressTextBox.Text;
        Account.PermissionUrl = PermissionUrlTextBox.Text;
        Account.TokenUrl = TokenUrlTextBox.Text;
        Account.OAuth2Scope = OAuth2ScopeTextBox.Text;
        Account.RedirectUri = RedirectUriTextBox.Text;
        Account.ClientId = ClientIdTextBox.Text;
        Account.ClientSecret = ClientSecretTextBox.Text;
        Account.OAuth2Flow = (OAuth2Flow)(OAuth2FlowComboBox.SelectedItem ?? OAuth2Flow.AuthorizationCode);
        Account.UsePkce = UsePkceCheckBox.IsChecked ?? false;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(EmailAddressTextBox.Text))
        {
            MessageBox.Show("Email address is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(ClientIdTextBox.Text))
        {
            MessageBox.Show("Client ID is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SaveAccountFromForm();
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
