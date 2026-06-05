@echo off
setlocal EnableExtensions

rem ---- Dedicated server config ----
rem Adjust these values before starting the server.
if not defined PORT set "PORT=7777"
if not defined MAX_PLAYERS set "MAX_PLAYERS=8"
if not defined SERVER_NAME set "SERVER_NAME=Derail Valley Dedicated Server"
if not defined PASSWORD set "PASSWORD="
if not defined DETAILS set "DETAILS=Started by start-dedicated-server.bat"
if not defined VISIBILITY set "VISIBILITY=Public"
if not defined PUBLIC_GAME set "PUBLIC_GAME=true"
if not defined HOST_TRANSPORT_MODE set "HOST_TRANSPORT_MODE=Direct"
rem Set HEADLESS=false for debugging with a visible window/menu.
if not defined HEADLESS set "HEADLESS=true"

rem ---- Paths ----
rem Default: script is in <Derail Valley>\Mods\Multiplayer\scripts.
set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "MOD_DIR=%%~fI"
for %%I in ("%MOD_DIR%\..\..") do set "GAME_DIR=%%~fI"
if not defined GAME_EXE set "GAME_EXE=%GAME_DIR%\DerailValley.exe"
if not defined LOG_FILE set "LOG_FILE=%GAME_DIR%\dedicated-server.log"
set "CONFIG_FILE=%MOD_DIR%\dedicated-server.json"

if not exist "%GAME_EXE%" (
  echo Game executable not found: "%GAME_EXE%"
  echo Set GAME_EXE=C:\Path\To\DerailValley.exe before running this script.
  exit /b 1
)

> "%CONFIG_FILE%" echo {
>>"%CONFIG_FILE%" echo   "serverName": "%SERVER_NAME%",
>>"%CONFIG_FILE%" echo   "password": "%PASSWORD%",
>>"%CONFIG_FILE%" echo   "details": "%DETAILS%",
>>"%CONFIG_FILE%" echo   "port": %PORT%,
>>"%CONFIG_FILE%" echo   "maxPlayers": %MAX_PLAYERS%,
>>"%CONFIG_FILE%" echo   "visibility": "%VISIBILITY%",
>>"%CONFIG_FILE%" echo   "publicGame": %PUBLIC_GAME%,
>>"%CONFIG_FILE%" echo   "hostTransportMode": "%HOST_TRANSPORT_MODE%"
>>"%CONFIG_FILE%" echo }

echo Wrote dedicated server config: "%CONFIG_FILE%"
echo Starting Derail Valley dedicated server on port %PORT% ...
echo Log file: "%LOG_FILE%"
echo Press Ctrl+C to stop the server.

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference = 'Stop';" ^
  "$gameExe = $env:GAME_EXE;" ^
  "$gameDir = $env:GAME_DIR;" ^
  "$logFile = $env:LOG_FILE;" ^
  "$headless = $env:HEADLESS;" ^
  "Set-Location -LiteralPath $gameDir;" ^
  "Remove-Item -LiteralPath $logFile -Force -ErrorAction SilentlyContinue;" ^
  "$args = @('-dvmp-dedicated', '-logFile', ('\"' + $logFile + '\"'));" ^
  "if ($headless -ieq 'true') { $args = @('-batchmode', '-nographics') + $args };" ^
  "$argumentLine = [string]::Join(' ', $args);" ^
  "$psi = New-Object System.Diagnostics.ProcessStartInfo;" ^
  "$psi.FileName = $gameExe;" ^
  "$psi.WorkingDirectory = $gameDir;" ^
  "$psi.Arguments = $argumentLine;" ^
  "$psi.UseShellExecute = $false;" ^
  "$proc = [System.Diagnostics.Process]::Start($psi);" ^
  "$fs = $null; $sr = $null;" ^
  "function Open-SharedLog([string]$path) { for ($i = 0; $i -lt 300; $i++) { try { if (Test-Path -LiteralPath $path) { return [System.IO.File]::Open($path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite -bor [System.IO.FileShare]::Delete) } } catch { } if ($proc.HasExited) { break }; Start-Sleep -Milliseconds 100 }; return $null };" ^
  "try {" ^
  "  while (-not $proc.HasExited -and -not $fs) { $fs = Open-SharedLog $logFile };" ^
  "  if ($fs) { $sr = New-Object System.IO.StreamReader($fs); while (-not $proc.HasExited) { while (($line = $sr.ReadLine()) -ne $null) { Write-Host $line }; Start-Sleep -Milliseconds 100 }; while (($line = $sr.ReadLine()) -ne $null) { Write-Host $line } } else { Write-Host ('Log file was not created before the server exited. Exit code: ' + $proc.ExitCode); $defaultLog = Join-Path $env:USERPROFILE 'AppData\LocalLow\Altfuture\Derail Valley\Player.log'; if (Test-Path -LiteralPath $defaultLog) { Write-Host ''; Write-Host ('Last lines from Unity default log: ' + $defaultLog); Get-Content -LiteralPath $defaultLog -Tail 80 } }" ^
  "}" ^
  "finally { if ($sr) { $sr.Dispose() }; if ($fs) { $fs.Dispose() }; if ($proc -and -not $proc.HasExited) { Write-Host ''; Write-Host 'Stopping Derail Valley dedicated server ...'; try { $proc.CloseMainWindow() | Out-Null } catch { }; Start-Sleep -Seconds 2; if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force } } }" ^
  "exit $proc.ExitCode"

exit /b %ERRORLEVEL%
