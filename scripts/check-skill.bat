@echo off
setlocal EnableExtensions
rem Checks this skill repository: token-budget line counts and text formatting.
rem Used by CI on every push; safe to run locally.
dotnet run "%~dp0check-skill.cs" -- "%~dp0.."
exit /b %ERRORLEVEL%
