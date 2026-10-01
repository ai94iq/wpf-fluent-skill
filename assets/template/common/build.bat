@echo off
setlocal EnableExtensions
cd /d "%~dp0"
set "RESULT=0"
set "OUT=artifacts\publish\win-x64"

rem Full output goes to artifacts\logs\build.log; the screen gets one line, or the
rem error lines plus the log path on failure.
if not exist "artifacts\logs" mkdir "artifacts\logs"

if exist "%OUT%" rmdir /s /q "%OUT%"
dotnet publish "src\__Product__.App\__Product__.App.csproj" -c Release -r win-x64 --self-contained true -p:PublishReadyToRun=true -o "%OUT%" -v q -nologo > "artifacts\logs\build.log" 2>&1
if errorlevel 1 goto :fail

echo [build] OK: %OUT%\__Product__.exe
goto :end

:fail
set "RESULT=1"
echo [build] FAILED - error lines below, full output in artifacts\logs\build.log
call logs.bat build error --no-pause

:end
if /i not "%~1"=="--no-pause" type "artifacts\logs\build.log"
if /i not "%~1"=="--no-pause" pause
exit /b %RESULT%
