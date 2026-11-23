@echo off
if exist "%~dp0Builds\Playtest-0.0.8\Hide iT.exe" (
  start "Hide iT" "%~dp0Builds\Playtest-0.0.8\Hide iT.exe" -force-d3d11
  exit /b
)
if exist "%~dp0Builds\Playtest-0.0.7\Hide iT.exe" (
  start "Hide iT" "%~dp0Builds\Playtest-0.0.7\Hide iT.exe" -force-d3d11
  exit /b
)
if exist "%~dp0Builds\Playtest-0.0.6\Hide iT.exe" (
  start "Hide iT" "%~dp0Builds\Playtest-0.0.6\Hide iT.exe" -force-d3d11
  exit /b
)
if exist "%~dp0Builds\Playtest-0.0.5\Hide iT.exe" (
  start "Hide iT" "%~dp0Builds\Playtest-0.0.5\Hide iT.exe" -force-d3d11
  exit /b
)
if exist "%~dp0Builds\Playtest-0.0.4\Hide iT.exe" (
  start "Hide iT" "%~dp0Builds\Playtest-0.0.4\Hide iT.exe" -force-d3d11
  exit /b
)
if exist "%~dp0Builds\Playtest-0.0.3\Hide iT.exe" (
  start "Hide iT" "%~dp0Builds\Playtest-0.0.3\Hide iT.exe" -force-d3d11
  exit /b
)
if not exist "%~dp0Builds\Playtest-0.0.2\Hide iT.exe" (
  echo Play Test 0.0.2 is not built yet. The previous tested ZIP is in Builds.
  pause
  exit /b 1
)
start "Hide iT" "%~dp0Builds\Playtest-0.0.2\Hide iT.exe" -force-d3d11
