# Email OAuth 2.0 Proxy - Uninstallation Script
# This script removes the Email OAuth 2.0 Proxy Windows Service

# Requires Administrator privileges
#Requires -RunAsAdministrator

param(
    [Parameter(Mandatory=$false)]
    [string]$ServiceName = "EmailOAuth2ProxyService",
    
    [Parameter(Mandatory=$false)]
    [switch]$KeepConfig = $false
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Email OAuth 2.0 Proxy - Uninstallation" -ForegroundColor Cyan
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

# Check if service exists
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if (-not $service) {
    Write-Host "Service '$ServiceName' is not installed" -ForegroundColor Yellow
    exit 0
}

Write-Host "Found service: $($service.DisplayName)" -ForegroundColor Cyan
Write-Host "Status: $($service.Status)" -ForegroundColor Cyan
Write-Host ""

$response = Read-Host "Are you sure you want to uninstall the service? (Y/N)"

if ($response -ne 'Y' -and $response -ne 'y') {
    Write-Host "Uninstallation cancelled" -ForegroundColor Yellow
    exit 0
}

# Stop the service if running
if ($service.Status -eq 'Running') {
    Write-Host "Stopping service..." -ForegroundColor Cyan
    Stop-Service -Name $ServiceName -Force
    Start-Sleep -Seconds 2
    Write-Host "Service stopped" -ForegroundColor Green
}

# Remove the service
Write-Host "Removing service..." -ForegroundColor Cyan
$result = & sc.exe delete $ServiceName

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to remove service" -ForegroundColor Red
    Write-Host $result -ForegroundColor Red
    exit 1
}

Write-Host "Service removed successfully" -ForegroundColor Green

# Ask about configuration
if (-not $KeepConfig) {
    Write-Host ""
    $configPath = Join-Path $env:APPDATA "EmailOAuth2Proxy"
    
    if (Test-Path $configPath) {
        Write-Host "Configuration found at: $configPath" -ForegroundColor Cyan
        $removeConfig = Read-Host "Do you want to remove configuration files? (Y/N)"
        
        if ($removeConfig -eq 'Y' -or $removeConfig -eq 'y') {
            Remove-Item -Path $configPath -Recurse -Force
            Write-Host "Configuration removed" -ForegroundColor Green
        } else {
            Write-Host "Configuration kept at: $configPath" -ForegroundColor Yellow
        }
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Uninstallation Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
