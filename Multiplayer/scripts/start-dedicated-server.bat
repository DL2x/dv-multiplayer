@echo off
setlocal EnableExtensions

rem ---- Dedicated server config ----
rem Adjust these values before starting the server.
set "PORT=7777"
set "MAX_PLAYERS=8"
set "SERVER_NAME=Derail Valley Dedicated Server"
set "PASSWORD="
set "DETAILS=Started by start-dedicated-server.bat"
set "VISIBILITY=Public"
set "PUBLIC_GAME=true"
set "HOST_TRANSPORT_MODE=Direct"
rem Set HEADLESS=false for debugging with a visible window/menu.
set "HEADLESS=true"

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
pushd "%GAME_DIR%"
if /I "%HEADLESS%"=="true" (
  "%GAME_EXE%" -batchmode -nographics -dvmp-dedicated -logFile "%LOG_FILE%"
) else (
  "%GAME_EXE%" -dvmp-dedicated -logFile "%LOG_FILE%"
)
set "EXIT_CODE=%ERRORLEVEL%"
popd
exit /b %EXIT_CODE%
