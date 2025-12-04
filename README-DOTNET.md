# Email OAuth 2.0 Proxy - .NET Windows Service Edition

This is a .NET conversion of the Email OAuth 2.0 Proxy that runs as a Windows Service with a WPF configuration GUI.

## Overview

The Email OAuth 2.0 Proxy has been converted from Python to .NET and restructured as:

- **EmailOAuth2Proxy.Core**: Core library containing proxy logic, configuration models, and services
- **EmailOAuth2Proxy.Service**: Windows Service that runs the proxy servers in the background
- **EmailOAuth2Proxy.ConfigGui**: WPF application for configuring the proxy and managing the Windows Service

## Prerequisites

- Windows 10 or later
- .NET 8.0 SDK or later
- Administrator privileges (for installing/managing the Windows Service)

## Building the Solution

1. Open a command prompt or PowerShell window
2. Navigate to the repository root directory
3. Build the solution:

```powershell
dotnet build EmailOAuth2Proxy.sln --configuration Release
```

## Configuration

### Using the GUI

1. Run the configuration GUI:
   ```powershell
   cd EmailOAuth2Proxy.ConfigGui\bin\Release\net8.0-windows
   .\EmailOAuth2Proxy.ConfigGui.exe
   ```

2. The GUI provides tabs for:
   - **Proxy Servers**: Configure IMAP/POP/SMTP proxy servers
   - **Email Accounts**: Add and configure email accounts with OAuth 2.0 settings
   - **Advanced Settings**: Configure advanced proxy options
   - **Service Control**: Install, start, stop, and uninstall the Windows Service

3. Configure your proxy servers and email accounts
4. Click "Save Configuration" to save your settings

### Manual Configuration

Configuration is stored in JSON format at:
```
%APPDATA%\EmailOAuth2Proxy\emailproxy.config.json
```

The configuration file includes:
- Server configurations (IMAP, POP, SMTP)
- Account configurations with OAuth 2.0 settings
- Advanced proxy options

## Installing and Running the Service

### Quick Installation (Using PowerShell Script)

1. Open PowerShell as Administrator
2. Navigate to the repository root
3. Run the installation script:
   ```powershell
   .\install.ps1
   ```

The script will:
- Check for the service executable
- Install the Windows Service
- Configure service recovery options
- Start the service
- Display next steps

To uninstall later:
```powershell
.\uninstall.ps1
```

### Using the GUI (Alternative Method)

1. Launch the configuration GUI as Administrator
2. Go to the "Service Control" tab
3. Click "Install Service" to install the Windows Service
4. Click "Start Service" to start the proxy

### Using Command Line (Manual Method)

1. Build the service:
   ```powershell
   dotnet publish EmailOAuth2Proxy.Service\EmailOAuth2Proxy.Service.csproj -c Release -r win-x64 --self-contained false
   ```

2. Install the service (run as Administrator):
   ```powershell
   sc create EmailOAuth2ProxyService binPath= "C:\path\to\EmailOAuth2Proxy.Service.exe" start= auto
   ```

3. Start the service:
   ```powershell
   sc start EmailOAuth2ProxyService
   ```

4. Check service status:
   ```powershell
   sc query EmailOAuth2ProxyService
   ```

## Configuring Email Accounts

### Office 365 / Outlook Example

1. Add a new account in the GUI
2. Configure:
   - **Email Address**: your.email@outlook.com
   - **Permission URL**: https://login.microsoftonline.com/common/oauth2/v2.0/authorize
   - **Token URL**: https://login.microsoftonline.com/common/oauth2/v2.0/token
   - **OAuth2 Scope**: https://outlook.office.com/IMAP.AccessAsUser.All https://outlook.office.com/POP.AccessAsUser.All https://outlook.office.com/SMTP.Send offline_access
   - **Redirect URI**: http://localhost
   - **Client ID**: Your registered client ID
   - **Client Secret**: Your client secret

### Gmail Example

1. Add a new account in the GUI
2. Configure:
   - **Email Address**: your.email@gmail.com
   - **Permission URL**: https://accounts.google.com/o/oauth2/auth
   - **Token URL**: https://oauth2.googleapis.com/token
   - **OAuth2 Scope**: https://mail.google.com/
   - **Redirect URI**: http://localhost
   - **Client ID**: Your registered client ID
   - **Client Secret**: Your client secret

## Default Server Configuration

The proxy comes pre-configured with common server settings:

### Office 365 / Outlook
- IMAP: localhost:1993 → outlook.office365.com:993
- POP: localhost:1995 → outlook.office365.com:995
- SMTP: localhost:1587 → smtp-mail.outlook.com:587 (STARTTLS)

### Gmail
- IMAP: localhost:2993 → imap.gmail.com:993
- POP: localhost:2995 → pop.gmail.com:995
- SMTP: localhost:2465 → smtp.gmail.com:465

## Email Client Configuration

Configure your email client to connect to:
- **Server**: 127.0.0.1 (localhost)
- **Port**: The local port from your server configuration (e.g., 1993 for Office 365 IMAP)
- **Security**: None/Unencrypted (the proxy handles encryption with the remote server)
- **Username**: Your email address
- **Password**: Any password (used only for encrypting cached tokens locally)

## Uninstalling the Service

### Using the GUI
1. Launch the configuration GUI as Administrator
2. Go to the "Service Control" tab
3. Click "Stop Service"
4. Click "Uninstall Service"

### Using Command Line
```powershell
sc stop EmailOAuth2ProxyService
sc delete EmailOAuth2ProxyService
```

## Troubleshooting

### Service Won't Start
- Ensure you have a valid configuration file
- Check Windows Event Viewer for error details (Application log)
- Verify you have the necessary permissions

### Configuration GUI Issues
- Run the GUI as Administrator to control the service
- Ensure .NET 8.0 runtime is installed

### Connection Issues
- Verify the proxy service is running
- Check that local ports aren't already in use
- Ensure firewall allows local connections

## Features

✅ **Windows Service**: Runs automatically in the background
✅ **WPF Configuration GUI**: Easy-to-use graphical interface
✅ **IMAP/POP/SMTP Support**: Full protocol support
✅ **OAuth 2.0 Flows**: Supports multiple OAuth 2.0 authentication flows
✅ **Multiple Accounts**: Manage multiple email accounts simultaneously
✅ **Secure Storage**: Configuration stored in user's AppData folder

## Differences from Python Version

This .NET version provides:
- Native Windows Service integration
- Modern WPF-based configuration GUI
- JSON-based configuration (vs INI-style in Python)
- Improved Windows integration

## Known Limitations

- Currently Windows-only (Windows Service and WPF are Windows-specific)
- OAuth 2.0 authentication flow requires manual token acquisition setup
- Some advanced Python proxy features may not be fully implemented

## Future Enhancements

- [ ] Full OAuth 2.0 authentication flow with browser popup
- [ ] Token refresh automation
- [ ] System tray integration
- [ ] Enhanced error handling and logging
- [ ] Support for more OAuth 2.0 providers
- [ ] Certificate-based authentication

## License

Apache 2.0 - Same as the original Python version

## Credits

Based on the Email OAuth 2.0 Proxy by Simon Robinson:
https://github.com/simonrob/email-oauth2-proxy
