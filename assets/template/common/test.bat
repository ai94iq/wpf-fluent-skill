@echo off
setlocal EnableExtensions
cd /d "%~dp0"
set "RESULT=0"

dotnet test "__Product__.slnx" -c Release -v q --nologo
if errorlevel 1 goto :fail

echo [test] OK
goto :end

:fail
set "RESULT=1"
echo [test] FAILED

:end
if /i not "%~1"=="--no-pause" pause
exit /b %RESULT%
