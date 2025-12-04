# Email OAuth 2.0 Proxy - Quick Start Guide (.NET Edition)

Get up and running with the Email OAuth 2.0 Proxy in 5 minutes!

## What You Need

- Windows 10 or later
- .NET 8.0 Runtime ([Download here](https://dotnet.microsoft.com/download/dotnet/8.0))
- Administrator access
- OAuth 2.0 client credentials from your email provider

## Step 1: Get the Software

**Option A: Download Pre-built Release (Coming Soon)**
- Visit the [Releases page](https://github.com/msee-dev/email-oauth2-proxy/releases)
- Download the latest release ZIP
- Extract to a folder like `C:\EmailOAuth2Proxy`

**Option B: Build from Source**
```powershell
git clone https://github.com/msee-dev/email-oauth2-proxy.git
cd email-oauth2-proxy
.\build.bat
```

## Step 2: Get OAuth Credentials

Before you can use the proxy, you need OAuth 2.0 credentials:

### For Office 365/Outlook:
1. Go to [Azure Portal](https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps)
2. Click "New registration"
3. Name: "Email Proxy" | Account types: "Personal and organizational" | Redirect: `http://localhost`
4. After creation, copy the **Application (client) ID**
5. Go to "Certificates & secrets" → Create new secret → Copy the **secret value**

### For Gmail:
1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a project → Enable Gmail API
3. Go to "Credentials" → "Create OAuth client ID" → Type: "Desktop app"
4. Copy the **Client ID** and **Client secret**

**Keep these credentials safe!**

## Step 3: Install & Configure

1. **Open PowerShell as Administrator**
   ```powershell
   # Navigate to the proxy folder
   cd C:\EmailOAuth2Proxy
   
   # Run installation script
   .\install.ps1
   ```

2. **Launch Configuration GUI** (automatically opens or run manually):
   ```powershell
   .\publish\ConfigGui\EmailOAuth2Proxy.ConfigGui.exe
   ```

3. **Add Your Email Account**:
   - Click "Email Accounts" tab
   - Click "Add Account"
   - Enter your email address
   - Paste your OAuth credentials:
     - For **Office 365**: Use settings from [Installation Guide](INSTALLATION-GUIDE.md#office-365--outlookcom)
     - For **Gmail**: Use settings from [Installation Guide](INSTALLATION-GUIDE.md#gmail--google-workspace)
   - Click "Save"

4. **Save Configuration**:
   - Click "Save Configuration" button at bottom
   - You should see a success message

5. **Start the Service** (if not auto-started):
   - Go to "Service Control" tab
   - Click "Start Service"
   - Verify status shows "Running"

## Step 4: Configure Your Email Client

Update your email client (Thunderbird, Outlook, etc.) with these settings:

### For Office 365 Account:
```
IMAP:
  Server: 127.0.0.1
  Port: 1993
  Security: None/Unencrypted
  
SMTP:
  Server: 127.0.0.1
  Port: 1587
  Security: None/Unencrypted
  
Username: your.email@outlook.com
Password: any-password-you-like (for token encryption only)
```

### For Gmail Account:
```
IMAP:
  Server: 127.0.0.1
  Port: 2993
  Security: None/Unencrypted
  
SMTP:
  Server: 127.0.0.1
  Port: 2465
  Security: None/Unencrypted
  
Username: your.email@gmail.com
Password: any-password-you-like (for token encryption only)
```

**Important Notes:**
- ✅ Use **localhost (127.0.0.1)** as the server
- ✅ Use **unencrypted/no security** in your email client
- ✅ The proxy handles secure connections to the real server
- ✅ The password is used only for local token encryption
- ✅ Use the **same password** across all clients for same account

## Step 5: First Connection

**Note**: The current version requires manual OAuth token setup. A future update will add automatic browser-based authentication.

For now, when your email client tries to connect:
1. The proxy will need valid OAuth tokens
2. You'll need to manually obtain and add tokens to the configuration
3. Or wait for the next version with automatic authentication

## Troubleshooting

### "Service failed to start"
- Check Event Viewer (Windows Logs → Application)
- Verify configuration file is valid
- Ensure ports aren't already in use

### "Can't connect to server"
- Verify proxy service is running (Service Control tab)
- Check you're using 127.0.0.1 and correct port
- Disable any firewall blocking localhost

### "Authentication failed"
- Current version requires manual token setup
- Verify your OAuth client credentials are correct
- Check credentials haven't expired

### Need More Help?
- Read [INSTALLATION-GUIDE.md](INSTALLATION-GUIDE.md)
- Check [README-DOTNET.md](README-DOTNET.md)
- Review [DOTNET-CONVERSION-SUMMARY.md](DOTNET-CONVERSION-SUMMARY.md)
- Open an issue on GitHub

## What's Next?

### Upcoming Features
- 🔜 Automatic browser-based OAuth authentication
- 🔜 System tray integration
- 🔜 MSI installer
- 🔜 Pre-built releases

### Learn More
- Full documentation: [README-DOTNET.md](README-DOTNET.md)
- Original Python version: [README.md](README.md)
- Technical details: [DOTNET-CONVERSION-SUMMARY.md](DOTNET-CONVERSION-SUMMARY.md)

## Uninstalling

To remove the proxy:
```powershell
# Open PowerShell as Administrator
.\uninstall.ps1
```

## Getting Support

1. Check the troubleshooting section above
2. Review the full [Installation Guide](INSTALLATION-GUIDE.md)
3. Search existing GitHub issues
4. Open a new issue with:
   - Your configuration (remove sensitive data)
   - Error messages from Event Viewer
   - Steps to reproduce

---

**Welcome to Email OAuth 2.0 Proxy!** 🎉

You're now ready to use your email client with OAuth 2.0-protected accounts without giving up your favorite email client.
