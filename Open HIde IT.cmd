@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\unity.ps1" -Action Open
if errorlevel 1 pause
