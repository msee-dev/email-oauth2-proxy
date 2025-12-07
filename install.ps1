# Email OAuth 2.0 Proxy - Installation Script
# This script automates the installation of the Email OAuth 2.0 Proxy Windows Service

# Requires Administrator privileges
#Requires -RunAsAdministrator

param(
    [Parameter(Mandatory=$false)]
    [string]$ServicePath = ".\publish\Service\EmailOAuth2Proxy.Service.exe",
    
    [Parameter(Mandatory=$false)]
    [string]$ServiceName = "EmailOAuth2ProxyService",
    
    [Parameter(Mandatory=$false)]
    [string]$DisplayName = "Email OAuth 2.0 Proxy Service",
    
    [Parameter(Mandatory=$false)]
    [string]$Description = "Transparently adds OAuth 2.0 support to IMAP/POP/SMTP client applications"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Email OAuth 2.0 Proxy - Installation" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
$isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Please right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

# Check if service path exists
if (-not (Test-Path $ServicePath)) {
    Write-Host "ERROR: Service executable not found at: $ServicePath" -ForegroundColor Red
    Write-Host "Please build the solution first using build.bat" -ForegroundColor Yellow
    exit 1
}

$ServicePath = Resolve-Path $ServicePath

# Check if service already exists
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if ($existingService) {
    Write-Host "Service '$ServiceName' already exists" -ForegroundColor Yellow
    $response = Read-Host "Do you want to reinstall? (Y/N)"
    
    if ($response -eq 'Y' -or $response -eq 'y') {
        Write-Host "Stopping and removing existing service..." -ForegroundColor Yellow
        
        # Stop the service if running
        if ($existingService.Status -eq 'Running') {
            Stop-Service -Name $ServiceName -Force
            Write-Host "Service stopped" -ForegroundColor Green
        }
        
        # Remove the service
        & sc.exe delete $ServiceName | Out-Null
        Start-Sleep -Seconds 2
        Write-Host "Service removed" -ForegroundColor Green
    } else {
        Write-Host "Installation cancelled" -ForegroundColor Yellow
        exit 0
    }
}

# Install the service
Write-Host "Installing service..." -ForegroundColor Cyan
$result = & sc.exe create $ServiceName binPath= "`"$ServicePath`"" start= auto DisplayName= "`"$DisplayName`""

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to install service" -ForegroundColor Red
    Write-Host $result -ForegroundColor Red
    exit 1
}

Write-Host "Service installed successfully" -ForegroundColor Green

# Set service description
& sc.exe description $ServiceName "$Description" | Out-Null

# Configure service recovery options (restart on failure)
Write-Host "Configuring service recovery options..." -ForegroundColor Cyan
& sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null
Write-Host "Recovery options configured" -ForegroundColor Green

# Start the service
Write-Host "Starting service..." -ForegroundColor Cyan
$startResult = & sc.exe start $ServiceName

if ($LASTEXITCODE -ne 0) {
    Write-Host "WARNING: Service installed but failed to start" -ForegroundColor Yellow
    Write-Host "You may need to configure the proxy before starting the service" -ForegroundColor Yellow
    Write-Host $startResult -ForegroundColor Yellow
} else {
    Write-Host "Service started successfully" -ForegroundColor Green
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Installation Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Run the Configuration GUI to set up your email accounts" -ForegroundColor White
Write-Host "   Location: .\publish\ConfigGui\EmailOAuth2Proxy.ConfigGui.exe" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Configure your email client to use the proxy" -ForegroundColor White
Write-Host "   - Server: 127.0.0.1" -ForegroundColor Gray
Write-Host "   - Ports: See your server configuration" -ForegroundColor Gray
Write-Host "   - Security: None/Unencrypted" -ForegroundColor Gray
Write-Host ""
Write-Host "Service Management Commands:" -ForegroundColor Cyan
Write-Host "  Start:   sc start $ServiceName" -ForegroundColor Gray
Write-Host "  Stop:    sc stop $ServiceName" -ForegroundColor Gray
Write-Host "  Status:  sc query $ServiceName" -ForegroundColor Gray
Write-Host "  Remove:  sc delete $ServiceName" -ForegroundColor Gray
Write-Host ""
