@echo off
powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0build\scripts\perf\PerfCore.ps1""" %*"
exit /b %ErrorLevel%