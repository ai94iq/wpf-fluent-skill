@echo off
setlocal EnableExtensions
rem Adds outline icons to a project and regenerates Resources\Icons.g.cs.
rem Example: add-icons.bat C:\Projects\MyApp person_add filter
if "%~1"=="" (
  echo Usage: add-icons.bat ProjectRoot [icon_name ...]
  exit /b 1
)
dotnet run "%~dp0add-icons.cs" -- %*
exit /b %ERRORLEVEL%
