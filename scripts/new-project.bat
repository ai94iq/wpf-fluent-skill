@echo off
setlocal EnableExtensions
rem Scaffolds a complete project from the skill template in one command.
rem Example: new-project.bat MyApp "My Company" C:\Projects\MyApp
if "%~3"=="" (
  echo Usage: new-project.bat ProductName "Company Name" TargetFolder [--ui wpf^|winui] [--no-build]
  exit /b 1
)
dotnet run "%~dp0new-project.cs" -- "%~dp0..\assets\template" %*
exit /b %ERRORLEVEL%
