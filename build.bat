@echo off
REM Build script for Email OAuth 2.0 Proxy .NET Edition

echo Building Email OAuth 2.0 Proxy .NET Edition...
echo.

echo Building Core Library...
dotnet build EmailOAuth2Proxy.Core\EmailOAuth2Proxy.Core.csproj --configuration Release
if %ERRORLEVEL% neq 0 goto :error

echo.
echo Building Windows Service...
dotnet publish EmailOAuth2Proxy.Service\EmailOAuth2Proxy.Service.csproj --configuration Release --runtime win-x64 --self-contained false --output publish\Service
if %ERRORLEVEL% neq 0 goto :error

echo.
echo Building Configuration GUI...
dotnet publish EmailOAuth2Proxy.ConfigGui\EmailOAuth2Proxy.ConfigGui.csproj --configuration Release --runtime win-x64 --self-contained false --output publish\ConfigGui
if %ERRORLEVEL% neq 0 goto :error

echo.
echo ========================================
echo Build completed successfully!
echo ========================================
echo.
echo Service executable: publish\Service\EmailOAuth2Proxy.Service.exe
echo Config GUI executable: publish\ConfigGui\EmailOAuth2Proxy.ConfigGui.exe
echo.
goto :end

:error
echo.
echo ========================================
echo Build failed! Please check the errors above.
echo ========================================
exit /b 1

:end
