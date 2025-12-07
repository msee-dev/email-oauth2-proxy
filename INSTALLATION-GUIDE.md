# Email OAuth 2.0 Proxy - Installation Guide

## Prerequisites

- **Operating System**: Windows 10 or later
- **.NET Runtime**: .NET 8.0 Runtime or SDK
  - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
- **Administrator Access**: Required for installing and managing the Windows Service

## Installation Steps

### Step 1: Download or Build the Application

#### Option A: Download Pre-built Binaries (Recommended)
1. Go to the [Releases](https://github.com/msee-dev/email-oauth2-proxy/releases) page
2. Download the latest release ZIP file
3. Extract to a folder (e.g., `C:\Program Files\EmailOAuth2Proxy`)

#### Option B: Build from Source
1. Clone the repository:
   ```powershell
   git clone https://github.com/msee-dev/email-oauth2-proxy.git
   cd email-oauth2-proxy
   ```

2. Build the solution:
   ```powershell
   build.bat
   ```

3. The built files will be in the `publish` folder

### Step 2: Configure the Proxy

1. **Launch the Configuration GUI as Administrator**:
   - Right-click on `EmailOAuth2Proxy.ConfigGui.exe`
   - Select "Run as administrator"

2. **Configure Proxy Servers** (if needed):
   - Go to the "Proxy Servers" tab
   - The default configuration includes servers for Office 365 and Gmail
   - Add, edit, or remove servers as needed

3. **Add Your Email Accounts**:
   - Go to the "Email Accounts" tab
   - Click "Add Account"
   - Fill in the required information:
     - **Email Address**: Your email address
     - **Permission URL**: OAuth authorization endpoint
     - **Token URL**: OAuth token endpoint
     - **OAuth 2.0 Scope**: Required permissions
     - **Client ID**: Your OAuth client ID
     - **Client Secret**: Your OAuth client secret (if required)
   
   **See the "Provider-Specific Configuration" section below for common providers**

4. **Save Configuration**:
   - Click "Save Configuration"

### Step 3: Install and Start the Service

1. **In the Configuration GUI**:
   - Go to the "Service Control" tab
   - Click "Install Service"
   - Wait for confirmation message
   - Click "Start Service"
   - Verify the service status shows "Running"

### Step 4: Configure Your Email Client

Configure your email client (Thunderbird, Outlook, etc.) with these settings:

#### For Office 365 Accounts:
- **IMAP Server**: 127.0.0.1
- **IMAP Port**: 1993
- **IMAP Security**: None/Unencrypted
- **SMTP Server**: 127.0.0.1
- **SMTP Port**: 1587
- **SMTP Security**: None/Unencrypted
- **Username**: Your full email address
- **Password**: Any password (used only for local token encryption)

#### For Gmail Accounts:
- **IMAP Server**: 127.0.0.1
- **IMAP Port**: 2993
- **IMAP Security**: None/Unencrypted
- **SMTP Server**: 127.0.0.1
- **SMTP Port**: 2465
- **SMTP Security**: None/Unencrypted
- **Username**: Your full email address
- **Password**: Any password (used only for local token encryption)

**Important Notes**:
- Use "None" or "Unencrypted" connection in your email client
- The proxy handles secure connections to the email server
- The password can be anything - it's used only to encrypt cached tokens locally
- The proxy must be running for your email client to connect

## Provider-Specific Configuration

### Office 365 / Outlook.com

**Pre-requisites**:
1. Register an application in Azure AD
   - Go to: https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps
   - Click "New registration"
   - Name: "Email OAuth2 Proxy"
   - Supported account types: "Accounts in any organizational directory and personal Microsoft accounts"
   - Redirect URI: Web - `http://localhost`
   - Click "Register"
2. Note your **Application (client) ID**
3. Create a client secret (Certificates & secrets > New client secret)
4. Note the **client secret value** (shown only once)

**Configuration**:
```
Email Address: your.email@outlook.com
Permission URL: https://login.microsoftonline.com/common/oauth2/v2.0/authorize
Token URL: https://login.microsoftonline.com/common/oauth2/v2.0/token
OAuth 2.0 Scope: https://outlook.office.com/IMAP.AccessAsUser.All https://outlook.office.com/POP.AccessAsUser.All https://outlook.office.com/SMTP.Send offline_access
Redirect URI: http://localhost
Client ID: [Your Application ID]
Client Secret: [Your Client Secret]
```

### Gmail / Google Workspace

**Pre-requisites**:
1. Create a project in Google Cloud Console
   - Go to: https://console.cloud.google.com/
   - Create a new project
2. Enable Gmail API
   - Go to "APIs & Services" > "Library"
   - Search for "Gmail API"
   - Click "Enable"
3. Create OAuth 2.0 credentials
   - Go to "APIs & Services" > "Credentials"
   - Click "Create Credentials" > "OAuth client ID"
   - Application type: "Desktop app"
   - Name: "Email OAuth2 Proxy"
   - Click "Create"
4. Note your **Client ID** and **Client Secret**

**Configuration**:
```
Email Address: your.email@gmail.com
Permission URL: https://accounts.google.com/o/oauth2/auth
Token URL: https://oauth2.googleapis.com/token
OAuth 2.0 Scope: https://mail.google.com/
Redirect URI: http://localhost
Client ID: [Your Client ID]
Client Secret: [Your Client Secret]
```

## First-Time Authentication

The proxy now includes **browser-based OAuth 2.0 authentication** for easy setup!

### Using Browser Authentication (Recommended)

1. **Open the Configuration GUI**
2. **Go to the "Email Accounts" tab**
3. **Select your account** from the list
4. **Click "Authenticate Account"**
5. **Your default browser will open** to the OAuth login page
6. **Log in and grant permissions** when prompted
7. **The browser will show a success message** when complete
8. **Return to the Configuration GUI** - your account is now authenticated!

The proxy will automatically:
- Open the correct OAuth login page
- Receive the authentication callback
- Exchange the authorization code for access tokens
- Save the tokens securely in the configuration
- Refresh tokens automatically when they expire

### Authentication Tips

- **First-time setup**: Use the "Authenticate Account" button after adding a new account
- **Re-authentication**: If a token expires or is revoked, select the account and click "Authenticate Account" again
- **Multiple accounts**: Authenticate each account separately using the same process
- **Redirect URI**: Make sure your OAuth app's redirect URI is set to `http://localhost` (or match what you configured)

### Troubleshooting Authentication

**Browser doesn't open:**
- Check that you have a default browser configured
- Try opening the browser manually and pasting the URL (shown in error messages)

**"Invalid redirect URI" error:**
- Verify your OAuth app configuration matches the redirect URI in the account settings
- For localhost, ensure your OAuth app allows `http://localhost` (no port number needed for OAuth 2.0 flows)

**"Invalid client" error:**
- Double-check your Client ID and Client Secret
- Verify the OAuth app hasn't been disabled or deleted

**Token refresh issues:**
- Make sure you requested the correct scopes (including `offline_access` for Office 365)
- Verify your OAuth app has permission to issue refresh tokens

## Managing the Service

### Start the Service
```powershell
# Using Configuration GUI (recommended)
# - Open GUI as Administrator
# - Go to "Service Control" tab
# - Click "Start Service"

# Using Command Line
sc start EmailOAuth2ProxyService
```

### Stop the Service
```powershell
# Using Configuration GUI (recommended)
# - Open GUI as Administrator
# - Go to "Service Control" tab
# - Click "Stop Service"

# Using Command Line
sc stop EmailOAuth2ProxyService
```

### Check Service Status
```powershell
sc query EmailOAuth2ProxyService
```

### View Service Logs
- Open Windows Event Viewer
- Navigate to: Windows Logs > Application
- Filter by Source: "Email OAuth 2.0 Proxy Service"

### Uninstall the Service
```powershell
# Using Configuration GUI (recommended)
# - Open GUI as Administrator
# - Go to "Service Control" tab
# - Click "Stop Service"
# - Click "Uninstall Service"

# Using Command Line
sc stop EmailOAuth2ProxyService
sc delete EmailOAuth2ProxyService
```

## Troubleshooting

### Service Won't Start
1. Check Event Viewer for error messages
2. Verify configuration file exists and is valid
3. Ensure ports aren't already in use by another application
4. Run Configuration GUI as Administrator

### Email Client Can't Connect
1. Verify the service is running (check in Configuration GUI)
2. Ensure you're using the correct localhost address and port
3. Check that no firewall is blocking localhost connections
4. Verify the server configuration matches your provider

### Authentication Issues
1. Verify your OAuth 2.0 client credentials are correct
2. Check that required API scopes are enabled
3. Ensure your OAuth 2.0 application is not expired or disabled
4. Review Event Viewer logs for specific error messages

### Configuration Changes Not Taking Effect
1. Save the configuration in the Configuration GUI
2. Restart the service:
   - Stop the service
   - Wait a few seconds
   - Start the service again

## Advanced Configuration

### Custom Port Numbers
You can change the local port numbers in the "Proxy Servers" tab. Make sure to:
1. Use ports above 1023 (unless running as SYSTEM)
2. Use ports not already in use
3. Update your email client configuration to match

### Multiple Accounts on Same Provider
You can add multiple accounts for the same email provider. They can share the same proxy servers (just use different account entries).

### Secure Local Connections
For added security, you can enable TLS for local connections by:
1. Generating or obtaining an SSL certificate
2. Configuring `LocalCertificatePath` and `LocalKeyPath` in the server configuration
3. Setting `LocalStartTls = true`

## Configuration File Location

The configuration is stored at:
```
%APPDATA%\EmailOAuth2Proxy\emailproxy.config.json
```

You can edit this file directly if needed, but using the Configuration GUI is recommended.

## Additional Resources

- **Documentation**: [README-DOTNET.md](README-DOTNET.md)
- **Original Python Version**: [README.md](README.md)
- **Issue Tracker**: https://github.com/msee-dev/email-oauth2-proxy/issues
- **OAuth 2.0 Specification**: https://oauth.net/2/

## Support

For issues or questions:
1. Check the troubleshooting section above
2. Review the Event Viewer logs
3. Open an issue on GitHub with:
   - Your configuration (remove sensitive information)
   - Error messages from Event Viewer
   - Steps to reproduce the issue
