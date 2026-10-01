@echo off
setlocal EnableExtensions
rem Keyboard menu for the skill: scaffold, icons, project tools, guides and checks.
rem Example: scripts\tui.bat
cd /d "%~dp0.."
where dotnet >nul 2>nul || goto :missing
for /f "delims=" %%v in ('dotnet --version 2^>nul') do set "SDK=%%v"
echo %SDK% | findstr /b "10." >nul || goto :old
dotnet run "scripts\tui.cs" -- "%CD%" %*
exit /b %ERRORLEVEL%

:missing
echo [tui] .NET 10 SDK not found. Install it from https://dotnet.microsoft.com/download/dotnet/10.0
if not defined CI pause
exit /b 1

:old
echo [tui] .NET 10 SDK required, found %SDK%. Get it from https://dotnet.microsoft.com/download/dotnet/10.0
if not defined CI pause
exit /b 1
