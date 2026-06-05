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
rem Hide noisy Unity rendering/headless messages that are expected with -nographics.
if not defined FILTER_RENDER_LOGS set "FILTER_RENDER_LOGS=true"
rem Write only the filtered console output to disk. Set false for zero server-log writes.
if not defined WRITE_FILTERED_LOG set "WRITE_FILTERED_LOG=true"

rem ---- Paths ----
rem Default: script is in <Derail Valley>\Mods\Multiplayer\scripts.
set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "MOD_DIR=%%~fI"
for %%I in ("%MOD_DIR%\..\..") do set "GAME_DIR=%%~fI"
if not defined GAME_EXE set "GAME_EXE=%GAME_DIR%\DerailValley.exe"
rem This is now a small FILTERED log, not Unity's raw log.
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
echo Unity raw log: disabled ^(using -logFile -^)
if /I "%WRITE_FILTERED_LOG%"=="true" echo Filtered log file: "%LOG_FILE%"
echo Press Ctrl+C to stop the server.

set "PS_SCRIPT=%TEMP%\dvmp-dedicated-%RANDOM%-%RANDOM%.ps1"
> "%PS_SCRIPT%" echo $ErrorActionPreference = 'Continue'
>>"%PS_SCRIPT%" echo $gameExe = $env:GAME_EXE
>>"%PS_SCRIPT%" echo $gameDir = $env:GAME_DIR
>>"%PS_SCRIPT%" echo $logFile = $env:LOG_FILE
>>"%PS_SCRIPT%" echo $headless = $env:HEADLESS
>>"%PS_SCRIPT%" echo $filterRenderLogs = $env:FILTER_RENDER_LOGS
>>"%PS_SCRIPT%" echo $writeFilteredLog = $env:WRITE_FILTERED_LOG
>>"%PS_SCRIPT%" echo Set-Location -LiteralPath $gameDir
>>"%PS_SCRIPT%" echo $writer = $null
>>"%PS_SCRIPT%" echo if ($writeFilteredLog -ieq 'true') { Remove-Item -LiteralPath $logFile -Force -ErrorAction SilentlyContinue; $writer = New-Object System.IO.StreamWriter($logFile, $false, [System.Text.Encoding]::UTF8); $writer.AutoFlush = $true }
>>"%PS_SCRIPT%" echo $script:suppressStack = $false
>>"%PS_SCRIPT%" echo $script:skipNextFilename = $false
>>"%PS_SCRIPT%" echo $script:suppressResolutionList = $false
>>"%PS_SCRIPT%" echo function Emit([string]$line) {
>>"%PS_SCRIPT%" echo   if ($filterRenderLogs -ieq 'true') {
>>"%PS_SCRIPT%" echo     if ($script:skipNextFilename -and $line.StartsWith('(Filename:')) { $script:skipNextFilename = $false; return }
>>"%PS_SCRIPT%" echo     $script:skipNextFilename = $false
>>"%PS_SCRIPT%" echo     if ($script:suppressStack) { if ([string]::IsNullOrWhiteSpace($line)) { $script:suppressStack = $false }; return }
>>"%PS_SCRIPT%" echo     if ($script:suppressResolutionList) { if ($line -match '^[0-9]+\s*x\s*[0-9]+\s*$' -or $line -match '^[0-9]+\s+x\s+[0-9]+\s*$' -or $line.StartsWith('(Filename:') -or [string]::IsNullOrWhiteSpace($line)) { if ([string]::IsNullOrWhiteSpace($line)) { $script:suppressResolutionList = $false }; return } else { $script:suppressResolutionList = $false } }
>>"%PS_SCRIPT%" echo     if ($line -like 'Supported resolutions:*') { $script:suppressResolutionList = $true; $script:skipNextFilename = $true; return }
>>"%PS_SCRIPT%" echo     $drop = $false
>>"%PS_SCRIPT%" echo     $patterns = @(
>>"%PS_SCRIPT%" echo       'Mono path*','Mono config path*','Initialize engine version:*','[Subsystems] Discovering subsystems*','Forcing GfxDevice:*','GfxDevice:*','NullGfxDevice:*','    Version:*','    Renderer:*','    Vendor:*','Begin MonoManager ReloadAssembly*',
>>"%PS_SCRIPT%" echo       '[Manager] Injection*','[Manager] Initialize*','[Manager] Version:*','[Manager] OS:*','[Manager] Net Framework:*','[Manager] Unity Engine:*','[Manager] Game:*','[Manager] IsSupportOnSession*','[Manager] Mods path:*','[Manager] Parsing mods*','[Manager] Reading file*','[Manager] Sorting mods*','[Manager] Loading mods*','[Manager] FINISH*','[Manager] Checking updates*','[Manager] Spawning*','[Manager] Starting*','[Manager] The nexus api key*',
>>"%PS_SCRIPT%" echo       '- Completed reload*','UnloadTime:*','Unloading *','Total: *','Texture streaming is enabled*','[MemoryMonitoring]*','*Microsoft Media Foundation video decoding to texture disabled:*','*Fallback handler could not load library*','*WARNING: Shader Unsupported:*','*WARNING: Shader Did you use #pragma only_renderers*','*RenderTexture.Create failed: format unsupported for random writes*','*Kernel ''MergeInstancedIndirectBuffers'' not found*','*Shader ''Oculus/OVRMRCameraFrameLit'': fallback shader ''Alpha-Diffuse'' not found*','*uses * texture parameters, more than the * supported by the current graphics device.*',
>>"%PS_SCRIPT%" echo       '*The referenced script*','*A scripted object*','*Did you #ifdef UNITY_EDITOR*','*Duplicate save file UID*','*SaveLoadController session is null*','*RefreshData fallback to empty lists*','*Wrote game preferences configuration:*','*Wrote file: Users*','*Unable to find style ''miniButton''*','Creating * singleton instance*','Creating DV.*','*doesn''t implement AllowAutoCreate method*','[Globals] fetching default config*',
>>"%PS_SCRIPT%" echo       'Build version:*','Mod managers:*','Build destination:*','Build timestamp:*','Build number:*','Build type:*','Build GUID:*','App version:*','App identifier:*','Build tags:*','OS:*','CPU:*','CPU freq:*','RAM:*','GPU:*','GPU vendor:*','GPU memory:*','GeForce NOW:*','Steam Deck:*','Log timestamp:*',
>>"%PS_SCRIPT%" echo       'Command line args:*','*DerailValley.exe','-batchmode','-nographics','-dvmp-dedicated','-logFile','-','C:\Program Files*','[LocalizationLoader]*','Checking for save imports*','No new save imports*','Save importing phase done*','Check for unsaved difficulties*','Unsaved difficulties phase done*','ManualDataLoader:*','VR is not enabled*','UIMenuController proceeding*','Recalculating DVObjectModel caches*','Using ScenarioCRUD path*','RequestUnload on scene*','[SceneSwitcher]*','[bootstrap]*','[MainMenuMusicFadeout]*',
>>"%PS_SCRIPT%" echo       '[Loading] loading start game data*','[Loading] initializing vegetation*','[Loading] initializing terrains*','[Loading] loading railway layout*','[Loading] initializing railway visuals*','[Loading] loading game content*','[Loading] *','RailTrackRegistry found*','Junctions hashes match*','Junctions state loaded*'
>>"%PS_SCRIPT%" echo     )
>>"%PS_SCRIPT%" echo     foreach ($pattern in $patterns) { if ($line -like $pattern) { $drop = $true; break } }
>>"%PS_SCRIPT%" echo     if ($drop) { $script:skipNextFilename = $true; return }
>>"%PS_SCRIPT%" echo     if ($line -like '*ArgumentException: Kernel ''MergeInstancedIndirectBuffers'' not found.*' -or $line -like '*Failed to load user from directory*') { $script:suppressStack = $true; return }
>>"%PS_SCRIPT%" echo   }
>>"%PS_SCRIPT%" echo   Write-Host $line
>>"%PS_SCRIPT%" echo   if ($writer) { $writer.WriteLine($line) }
>>"%PS_SCRIPT%" echo }
>>"%PS_SCRIPT%" echo $gameArgs = @('-dvmp-dedicated', '-logFile', '-')
>>"%PS_SCRIPT%" echo if ($headless -ieq 'true') { $gameArgs = @('-batchmode', '-nographics') + $gameArgs }
>>"%PS_SCRIPT%" echo try {
>>"%PS_SCRIPT%" echo   ^& $gameExe @gameArgs 2^>^&1 ^| ForEach-Object { Emit ($_.ToString().TrimEnd()) }
>>"%PS_SCRIPT%" echo   $exitCode = if ($LASTEXITCODE -ne $null) { $LASTEXITCODE } else { 0 }
>>"%PS_SCRIPT%" echo } finally {
>>"%PS_SCRIPT%" echo   if ($writer) { $writer.Dispose() }
>>"%PS_SCRIPT%" echo }
>>"%PS_SCRIPT%" echo exit $exitCode

powershell -NoProfile -ExecutionPolicy Bypass -File "%PS_SCRIPT%"
set "EXIT_CODE=%ERRORLEVEL%"
del "%PS_SCRIPT%" >nul 2>nul
exit /b %EXIT_CODE%
