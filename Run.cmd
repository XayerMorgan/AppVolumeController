@echo off
cd /d "%~dp0"
if exist "artifacts\publish\win-x64\AppVolumeController.exe" (
 start "" "artifacts\publish\win-x64\AppVolumeController.exe"
) else (
 echo Build first with Build.ps1. See README.md.
 pause
)
