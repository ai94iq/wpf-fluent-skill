@echo off
setlocal EnableExtensions
rem Scaffolds a complete project from the skill template in one command.
rem Example: new-project.bat MyApp "My Company" C:\Projects\MyApp
if "%~3"=="" (
  echo Usage: new-project.bat ProductName "Company Name" TargetFolder [--ui wpf^|winui] [--no-build]
  exit /b 1
)
rem Resolve a relative target against the caller's folder, then run from the skill root so the
rem helper itself always compiles with the skill's .NET 10 SDK rather than the caller's.
for %%I in ("%~3") do set "TARGET=%%~fI"
pushd "%~dp0.."
dotnet run "%~dp0new-project.cs" -- "%~dp0..\assets\template" "%~1" "%~2" "%TARGET%" %4 %5 %6 %7 %8 %9
set "RESULT=%ERRORLEVEL%"
popd
exit /b %RESULT%
